using System;
using Mirror;

namespace ExplosiveFactory.Utils
{
    public readonly struct NetworkUuid : IEquatable<NetworkUuid>
    {
        public readonly byte PlayerId;
        public readonly uint SubId;

        private static uint _nextSubId;

        public static readonly NetworkUuid Empty = new(255, uint.MaxValue);
        public bool IsValid => PlayerId != 255 || SubId != uint.MaxValue;

        public static NetworkUuid Generate()
        {
            byte localPlayerId = 0;
            if (NetworkClient.connection != null)
            {
                localPlayerId = (byte)(NetworkClient.connection.connectionId & 0xFF);
            }

            return new NetworkUuid(localPlayerId, _nextSubId++);
        }

        public static NetworkUuid Generate(byte playerId)
        {
            return new NetworkUuid(playerId, _nextSubId++);
        }

        public NetworkUuid(byte playerId, uint subId)
        {
            PlayerId = playerId;
            SubId = subId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PlayerId, SubId);
        }

        public override bool Equals(object? obj)
        {
            return obj is NetworkUuid other && Equals(other);
        }

        public bool Equals(NetworkUuid other)
        {
            return PlayerId == other.PlayerId && SubId == other.SubId;
        }

        public override string ToString()
        {
            return $"NetworkUuid(PlayerId={PlayerId}, SubId={SubId})";
        }

        public static bool operator ==(NetworkUuid left, NetworkUuid right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NetworkUuid left, NetworkUuid right)
        {
            return !left.Equals(right);
        }
    }

    public static class NetworkUuidSerializer
    {
        public static void WriteNetworkUuid(this NetworkWriter writer, NetworkUuid value)
        {
            writer.WriteByte(value.PlayerId);
            writer.WriteUInt(value.SubId);
        }

        public static NetworkUuid ReadNetworkUuid(this NetworkReader reader)
        {
            return new NetworkUuid(reader.ReadByte(), reader.ReadUInt());
        }
    }
}
