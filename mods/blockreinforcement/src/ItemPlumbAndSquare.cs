using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace VSMods
{
    public class ItemPlumbAndSquare : Item
    {

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public override void OnHeldInteractStart(IItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                handling = EnumHandHandling.PreventDefaultAction;
                return;
            }
            if (blockSel == null)
            {
                return;
            }

            ModSystemBlockReinforcement bre = byEntity.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();

            IPlayer player = (byEntity as EntityPlayer).Player;
            if (player == null) return;

            ItemSlot resSlot = bre.FindResourceForReinforcing(player);
            if (resSlot == null) return;

            int strength = resSlot.Itemstack.ItemAttributes["reinforcementStrength"].AsInt(0);
            
            if (!bre.StrengthenBlock(blockSel.Position, player, strength))
            {
                (player as IServerPlayer).SendMessage(GlobalConstants.CurrentChatGroup, "Cannot reinforce block, it's already reinforced!", EnumChatType.Notification);
                return;
            }

            resSlot.TakeOut(1);
            resSlot.MarkDirty();

            BlockPos pos = blockSel.Position;
            byEntity.World.PlaySoundAt(new AssetLocation("blockreinforcement", "sounds/reinforce"), pos.X, pos.Y, pos.Z, null);

            handling = EnumHandHandling.PreventDefaultAction;
        }



        public override void OnHeldAttackStart(IItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                handling = EnumHandHandling.PreventDefaultAction;
                return;
            }

            if (blockSel == null)
            {
                return;
            }

            ModSystemBlockReinforcement bre = byEntity.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
            IServerPlayer player = (byEntity as EntityPlayer).Player as IServerPlayer;
            if (player == null) return;

            string errorCode = "";
            if (!bre.TryRemoveReinforcement(blockSel.Position, player, ref errorCode))
            {
                player.SendMessage(GlobalConstants.CurrentChatGroup, "Cannot remove reinforcement: " + errorCode, EnumChatType.Notification);
                return;
            }

            BlockPos pos = blockSel.Position;
            byEntity.World.PlaySoundAt(new AssetLocation("blockreinforcement", "sounds/reinforce"), pos.X, pos.Y, pos.Z, null);

            handling = EnumHandHandling.PreventDefaultAction;
        }
        
    }
}
