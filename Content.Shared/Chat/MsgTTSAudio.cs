using System;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.GameObjects;

namespace Content.Shared.Chat
{
    public sealed class MsgTTSAudio : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public byte[] Data { get; set; } = Array.Empty<byte>();
        public EntityUid Source { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            var length = buffer.ReadInt32();
            Data = buffer.ReadBytes(length);
            // Robust way to read EntityID
            var rawId = buffer.ReadInt32();
            Source = new EntityUid(rawId);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(Data.Length);
            buffer.Write(Data);
            buffer.Write((int)Source);
        }
    }
}
