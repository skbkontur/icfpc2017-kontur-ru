# ICFPC 2017. Team kontur.ru

## Инструкция для команды

Для простоты всю логику пишем в одном проекте lib.dll.
Создавайте в нем поддиректории под фичи, которыми занимаетесь.

Проекты с exe-файлами для простоты не должны содержать никакой логики, кроме, собственно запуска.

Уже подключены библиотеки NUnit, Shouldly, FluentAssertions, MoreLinq и Firebase.

Данные, которые нужно шарить между командой и обновлять предлагается хранить в Firebase
— он подключен, настроен. Есть FbClient с примером работы с Firebase.
Логин и пароль захардкожены ради удобства.

Репозиторий содержит Team-shared настройки решарпера. Перед коммитом рекомендуется
запускать реформат "Soft Cleanup" для всего нового кода, чтобы минимизировать конфликты при мерджах.


## Запуск параллельных вычислений на кластере

Сервис worker автоматически собирается по каждому коммиту и раскладывается на кластер. Воркеры получают задания
в произвольном формате через очередь Kafka и складывают куда-нибудь результат (можно в другую очередь Kafka, можно
в Firebase, можно куда угодно). Пример уже лежит в репозитории. Вместо засыпания на 15 секунд можно вставить
какое-нибудь полезное вычисление.


## Автотесты

https://tc.skbkontur.ru/project.html?projectId=icfpc&branch_icfpc=all_branches

Тесты, которые не нужно запускать надо помечать атрибутом [Explicit].
