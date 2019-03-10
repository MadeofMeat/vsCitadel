using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Server;

namespace VSMods
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class BlockReinforcement {
        public int Strength;
        public string PlayerUID;
    }

    public class ModSystemBlockReinforcement : ModSystem
    {
        ICoreAPI api;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return true;
        }

        public override void Start(ICoreAPI api)
        {
            this.api = api;

            api.RegisterItemClass("ItemPlumbAndSquare", typeof(ItemPlumbAndSquare));
            api.RegisterBlockBehaviorClass("Reinforcable", typeof(ReinforcableBlockBehavior));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.ServerRunPhase(EnumServerRunPhase.LoadGamePre, addReinforcementBehavior);
        }
        

        private void addReinforcementBehavior()
        {
            foreach (Block block in api.World.Blocks)
            {
                if (block.Code == null) continue;
                if (block.Attributes == null || block.Attributes["reinforcable"].AsBool(true) != false)
                {
                    block.BlockBehaviors = block.BlockBehaviors.Append(new ReinforcableBlockBehavior(block));
                }
            }
        }


        public ItemSlot FindResourceForReinforcing(IPlayer byPlayer)
        {
            ItemSlot foundSlot = null;

            byPlayer.Entity.WalkInventory((onSlot) =>
            {
                if (onSlot.Itemstack == null || onSlot.Itemstack.ItemAttributes == null) return true;

                int? strength = onSlot.Itemstack.ItemAttributes["reinforcementStrength"].AsInt(0);
                if (strength > 0)
                {
                    foundSlot = onSlot as ItemSlot;
                    return false;
                }

                return true;
            });

            return foundSlot;
        }


        public bool TryRemoveReinforcement(BlockPos pos, IPlayer byPlayer, ref string errorCode)
        {
            Dictionary<int, BlockReinforcement> reinforcmentsOfChunk = getOrCreateReinforcmentsAt(pos);

            int index3d = toLocalIndex(pos);
            if (!reinforcmentsOfChunk.ContainsKey(index3d))
            {
                errorCode = "notreinforced";
                return false;
            }

            if (reinforcmentsOfChunk[index3d].PlayerUID != byPlayer.PlayerUID)
            {
                errorCode = "notownblock";
                return false;
            }

            reinforcmentsOfChunk.Remove(index3d);

            saveReinforcments(reinforcmentsOfChunk, pos);
            return true;
        }

        public int GetRemainingStrength(BlockPos pos)
        {
            Dictionary<int, BlockReinforcement> reinforcmentsOfChunk = getOrCreateReinforcmentsAt(pos);

            int index3d = toLocalIndex(pos);
            if (!reinforcmentsOfChunk.ContainsKey(index3d)) return 0;

            return reinforcmentsOfChunk[index3d].Strength;
        }


        public void ConsumeStrength(BlockPos pos, int byAmount)
        {
            Dictionary<int, BlockReinforcement> reinforcmentsOfChunk = getOrCreateReinforcmentsAt(pos);

            int index3d = toLocalIndex(pos);
            if (!reinforcmentsOfChunk.ContainsKey(index3d)) return;

            reinforcmentsOfChunk[index3d].Strength--;
            saveReinforcments(reinforcmentsOfChunk, pos);
        }


        public bool StrengthenBlock(BlockPos pos, IPlayer byPlayer, int strength)
        {
            if (api.Side == EnumAppSide.Client) return false;

            Dictionary<int, BlockReinforcement> reinforcmentsOfChunk = getOrCreateReinforcmentsAt(pos);

            int index3d = toLocalIndex(pos);

            if (reinforcmentsOfChunk.ContainsKey(index3d)) return false;

            reinforcmentsOfChunk[index3d] = new BlockReinforcement() { PlayerUID = byPlayer.PlayerUID, Strength = strength };

            saveReinforcments(reinforcmentsOfChunk, pos);
            
            return true;
        }


        Dictionary<int, BlockReinforcement> getOrCreateReinforcmentsAt(BlockPos pos)
        {
            IServerChunk chunk = (api as ICoreServerAPI).WorldManager.GetChunk(pos);

            // Fix v1.8 game engine bug (can be removed for v1.9)
            if ((chunk as ServerChunk).Moddata == null) (chunk as ServerChunk).Moddata = new Dictionary<string, byte[]>();

            byte[] data = chunk.GetModdata("reinforcements");
            Dictionary<int, BlockReinforcement> reinforcmentsOfChunk = null;

            if (data != null)
            {
                reinforcmentsOfChunk = SerializerUtil.Deserialize<Dictionary<int, BlockReinforcement>>(data);
            }
            else
            {
                reinforcmentsOfChunk = new Dictionary<int, BlockReinforcement>();
            }

            return reinforcmentsOfChunk;
        }

        void saveReinforcments(Dictionary<int, BlockReinforcement> reif, BlockPos pos)
        {
            IServerChunk chunk = (api as ICoreServerAPI).WorldManager.GetChunk(pos);
            
            chunk.SetModdata("reinforcements", SerializerUtil.Serialize(reif));
        }


        int toLocalIndex(BlockPos pos)
        {
            return toLocalIndex(pos.X % api.World.BlockAccessor.ChunkSize, pos.Y % api.World.BlockAccessor.ChunkSize, pos.Z % api.World.BlockAccessor.ChunkSize);
        }

        int toLocalIndex(int x, int y, int z)
        {
            return (y << 16) | (z << 8) | (x);
        }

        Vec3i fromLocalIndex(int index)
        {
            return new Vec3i(index & 0xff, (index >> 16) & 0xff, (index >> 8) & 0xff);
        }

    }
}
