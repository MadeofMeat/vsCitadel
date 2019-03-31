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
    public class ReinforcableBlockBehavior : BlockBehavior
    {
        public ReinforcableBlockBehavior(Block block) : base(block)
        {
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            if (world.Side == EnumAppSide.Client)
            {
                handling = EnumHandling.PreventDefault;
                return;
            }

            ModSystemBlockReinforcement bre = world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();

            int strength = bre.GetRemainingStrength(pos);

            (byPlayer as IServerPlayer).SendMessage(GlobalConstants.DamageLogChatGroup, "Strength left: " + strength, EnumChatType.Notification);


            if (strength > 0)
            {
                handling = EnumHandling.PreventDefault;
                
                world.PlaySoundAt(new AssetLocation("blockreinforcement", "sounds/breakreinforced"), pos.X, pos.Y, pos.Z, null);

                bre.ConsumeStrength(pos, 1);

                world.BlockAccessor.MarkBlockDirty(pos);
            }
        }
        
    }
}
