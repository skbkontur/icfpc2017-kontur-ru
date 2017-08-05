﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using lib.Arena;
using lib.OnlineRunner;
using Newtonsoft.Json;
using NLog;

namespace worker
{
    public class WorkerService
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, object> config;
        private readonly string inputTopicName;
        private readonly string outputTopicName;
        private readonly IExperiment experiment;
        private bool cancelled;
        private Thread workerThread;
        private Thread arenaThread;
        private string commitHash;

        public WorkerService(Dictionary<string, object> conf, string input, string output, IExperiment experiment)
        {
            config = conf;
            inputTopicName = input;
            outputTopicName = output;
            this.experiment = experiment;
            try
            {
                commitHash = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "commit_hash.txt"))
                    .FirstOrDefault();
            }
            catch (Exception e)
            {
                commitHash = "Unknown";
                logger.Warn("Can't read commit_hash.txt");
                logger.Error(e);
            }
        }

        public void Start()
        {
            arenaThread = new Thread(
                () =>
                {
                    while (!cancelled)
                    {
                        OnlineArenaRunner.TryCompeteOnArena("TCWorker", commitHash);
                    }
                });
            arenaThread.Start();

            workerThread = new Thread(
                () =>
                {
                    using (var consumer = new Consumer<Null, string>(
                        config, null, new StringDeserializer(Encoding.UTF8)))
                    using (var producer = new Producer<Null, string>(config, null, new StringSerializer(Encoding.UTF8)))
                    {
                        consumer.OnPartitionEOF += (_, end) => logger.Info(
                            $"Reached end of topic {end.Topic} partition {end.Partition}, next message will be at offset {end.Offset}");

                        consumer.OnError += (_, error) => logger.Error($"Error: {error}");

                        consumer.OnPartitionsAssigned += (_, partitions) =>
                        {
                            logger.Info(
                                $"Assigned partitions: [{string.Join(", ", partitions)}], member id: {consumer.MemberId}");
                            consumer.Assign(partitions);
                        };

                        consumer.OnPartitionsRevoked += (_, partitions) =>
                        {
                            logger.Info($"Revoked partitions: [{string.Join(", ", partitions)}]");
                            consumer.Unassign();
                        };

                        consumer.OnStatistics += (_, json) => logger.Info($"Statistics: {json}");

                        consumer.Subscribe(inputTopicName);

                        while (!cancelled)
                        {
                            Message<Null, string> msg;

                            if (!consumer.Consume(out msg, TimeSpan.FromSeconds(1)))
                            {
                                continue;
                            }

                            logger.Info(
                                $"Got message | Topic: {msg.Topic} Partition: {msg.Partition} Offset: {msg.Offset} {msg.Value}");

                            try
                            {
                                var task = JsonConvert.DeserializeObject<Task>(msg.Value);
                                Result result = null;
                                try
                                {
                                    result = experiment.Play(task);
                                }
                                catch (Exception exception)
                                {
                                    result = new Result {Error = exception.Message};
                                }
                                result.Task = task;
                                result.Token = task.Token;
                                var resultString = JsonConvert.SerializeObject(result);

                                var deliveryReport = producer.ProduceAsync(outputTopicName, null, resultString);

                                deliveryReport.ContinueWith(
                                    x =>
                                    {
                                        logger.Info(
                                            $"Sent result | Partition: {x.Result.Partition}, Offset: {x.Result.Offset}");
                                    });
                            }
                            catch (Exception e)
                            {
                                logger.Warn(e);
                            }
                        }

                        producer.Flush(TimeSpan.FromSeconds(10));
                    }
                });
            workerThread.Start();
        }

        public void Stop()
        {
            cancelled = true;
            workerThread.Join();
            arenaThread.Join();
        }
    }
}