using System;
using System.IO;

namespace lib.Structures
{
    public class Move : IEquatable<Move>
    {
        public ClaimMove claim;
        public PassMove pass;

        public static Move Claim(int punter, int source, int target)
        {
            return new Move { claim = new ClaimMove { punter = punter, source = source, target = target } };
        }

        public static Move Pass(int punter)
        {
            return new Move {pass = new PassMove {punter = punter}};
        }

        public override string ToString()
        {
            if (claim != null)
                return claim.ToString();
            if (pass != null)
                return pass.ToString();
            return "Invalid move";
        }

        public bool Equals(Move other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(claim, other.claim) && Equals(pass, other.pass);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Move) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((claim != null ? claim.GetHashCode() : 0) * 397) ^ (pass != null ? pass.GetHashCode() : 0);
            }
        }

        public static bool operator ==(Move left, Move right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Move left, Move right)
        {
            return !Equals(left, right);
        }

        public static Move DecodeFrom(BinaryReader reader)
        {
            byte marker = reader.ReadByte();
            if (marker == 0) return null;
            int punter = reader.ReadInt32();
            int source = reader.ReadInt32();
            int target = reader.ReadInt32();
            return Claim(punter, source, target);
        }

        public void EncodeTo(BinaryWriter w)
        {
            if (claim == null) return;
            w.Write((byte)1);
            w.Write(claim.punter);
            w.Write(claim.source);
            w.Write(claim.target);
        }
    }
}