using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace WeaponDamageInherit.Content
{
    public class InheritAnvil : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ShimmerTransformToItem[ItemID.IronAnvil] = Type;
            ItemID.Sets.ShimmerTransformToItem[ItemID.LeadAnvil] = Type;
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            Item.useStyle = 1;
            Item.useTurn = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.autoReuse = true;
            Item.maxStack = Terraria.Item.CommonMaxStack;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<InheritAnvil_Tile>();
            Item.width = 28;
            Item.height = 14;
            Item.value = 7600;
            Item.rare = ItemRarityID.Expert;
            base.SetDefaults();
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D glowTexture = ModContent.Request<Texture2D>("WeaponDamageInherit/Content/InheritAnvil_RainbowMask").Value;
            Vector2 basePosition = position + new Vector2(0, -2f * scale);
            float baseScale = 1.1f;
            Color baseColor = Color.White with { A = 0 } * 0.5f;
            spriteBatch.Draw(glowTexture, 
                basePosition, 
                frame, 
                baseColor, 
                0f, 
                origin, 
                scale * baseScale, 
                SpriteEffects.None, 
                0f);
            for (int n = 0; n < 3; n++)
            {
                float t = (n * 0.33f + Main.GlobalTimeWrappedHourly) % 1f;
                float k = t * (1 - t);
                float verticalOffset = -4f * t;
                float pulseScale = 1f + t;

                spriteBatch.Draw(glowTexture,
                    basePosition + new Vector2(0f, verticalOffset),
                    frame,
                    Main.DiscoColor with { A = 0 } * k,
                    0f,
                    origin,
                    scale * pulseScale,
                    SpriteEffects.None,
                    0f);
            }

            return true;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D glowTexture = ModContent.Request<Texture2D>("WeaponDamageInherit/Content/InheritAnvil_RainbowMask").Value;
            Vector2 drawPosition = Item.Center - Main.screenPosition;
            Vector2 origin = new Vector2(Item.width / 2f, Item.height / 2f);
            float baseScale = 1.1f;
            spriteBatch.Draw(glowTexture,
                drawPosition,
                null,
                Color.White with { A = 0 } * 0.5f,
                rotation,
                origin,
                scale * baseScale,
                SpriteEffects.None,
                0f);

            for (int n = 0; n < 3; n++)
            {
                float t = (n * 0.33f + Main.GlobalTimeWrappedHourly) % 1f;
                float k = t * (1 - t);
                float verticalOffset = -4f * t * scale;
                float pulseScale = 1f + t;

                spriteBatch.Draw(glowTexture,
                    drawPosition + new Vector2(0f, verticalOffset),
                    null,
                    Main.DiscoColor with { A = 0 } * k,
                    rotation,
                    origin,
                    scale * pulseScale,
                    SpriteEffects.None,
                    0f);
            }
            return true;
        }
    }

    public class InheritAnvil_Tile : ModTile
    {
        public override void SetStaticDefaults()
        {
            // Properties
            Main.tileTable[Type] = true;
            Main.tileSolidTop[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileFrameImportant[Type] = true;
            TileID.Sets.IgnoredByNpcStepUp[Type] = true; 
            AdjTiles = new int[] { TileID.Anvils };
            DustType = DustID.ShimmerSpark;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x1);
            TileObjectData.newTile.CoordinateHeights = new[] { 18 };
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(0, 0, 200), Language.GetText("ItemName.WorkBench"));
            Main.tileLighted[Type] = true;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 0.5f;
            g = 0.5f;
            b = 0.5f;
        }

        public override void NumDust(int x, int y, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }
        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            base.KillTile(i, j, ref fail, ref effectOnly, ref noItem);
        }
        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            Main.LocalPlayer.GetModPlayer<WeaponInheritAssitPlayer>().lastChoseTileCoord = null;
            WeaponInheritSystem.instance.weaponInheritUI.Close();

            base.KillMultiTile(i, j, frameX, frameY);
        }
        public override bool RightClick(int i, int j)
        {
            var ui = WeaponInheritSystem.instance.weaponInheritUI;
            if (WeaponInheritUI.Visible)
                ui.Close();
            else
            {
                ui.Open();
                Main.LocalPlayer.GetModPlayer<WeaponInheritAssitPlayer>().lastChoseTileCoord = new Point(i - Main.tile[i, j].TileFrameX / 16, j);
                //var vec = Main.MouseScreen;
                //var vec = new Vector2(i - 3,j - 6) * 16 - Main.screenPosition;

                //float zoom = Main.GameZoomTarget * Main.ForcedMinimumZoom;
                //vec = (vec - Main.ScreenSize.ToVector2() * .5f) * zoom + Main.ScreenSize.ToVector2() * .5f;
                //vec /= Main.UIScale;
                //ui.panel.Top.Set(vec.Y, 0);
                //ui.panel.Left.Set(vec.X, 0);
                //ui.Recalculate();
            }
            return true;
        }
        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Vector2 zero = new Vector2(Main.offScreenRange, Main.offScreenRange);
            if (Main.drawToScreen)
            {
                zero = Vector2.Zero;
            }
            var glowTexture = ModContent.Request<Texture2D>("WeaponDamageInherit/Content/InheritAnvil_Tile_RainbowMask");
            Tile tile = Main.tile[i, j];
            Vector2 origin = tile.TileFrameX != 0 ? new Vector2(0, 18) : new Vector2(14, 18);
            Vector2 pos = new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y) + zero + origin;
            Rectangle rect = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 18);
            spriteBatch.Draw(glowTexture.Value, pos, rect, Color.White with { A = 0 } * .5f, 0, origin, 1.25f, SpriteEffects.None, 0f);// + MathF.Cos(Main.GlobalTimeWrappedHourly)*.5f
            for (int n = 0; n < 3; n++)
            {
                float t = n * .33f + Main.GlobalTimeWrappedHourly;
                t %= 1;
                float k = t * (1 - t);
                spriteBatch.Draw(glowTexture.Value, pos + new Vector2(0, -4 * t), rect, Main.DiscoColor with { A = 0 } * k, 0, origin, 1 + t, 0, 0);//
            }
            var mplr = Main.LocalPlayer.GetModPlayer<WeaponInheritAssitPlayer>();
            var coord = mplr.lastChoseTileCoord;
            if (coord != null && coord.Value.X == i && coord.Value.Y == j)
            {
                float fs = (MathF.Cos(Main.GlobalTimeWrappedHourly * 2) + 1) * .2f;
                if (mplr.itemR != null && mplr.itemR.type != ItemID.None)
                {
                    var tex = TextureAssets.Item[mplr.itemR.type].Value;
                    spriteBatch.Draw(tex, pos + new Vector2(8, -64 + MathF.Cos(Main.GlobalTimeWrappedHourly) * 16), null, Main.DiscoColor with { A = 127 }, 0, tex.Size() * .5f, 1f + fs, 0, 0);

                    spriteBatch.Draw(tex, pos + new Vector2(8, -64 + MathF.Cos(Main.GlobalTimeWrappedHourly) * 16), null, Color.White, 0, tex.Size() * .5f, 1f, 0, 0);

                }
                else
                {
                    float ds = MathF.Cos(Main.GlobalTimeWrappedHourly * 2) * .1f;
                    if (ds > 0 && mplr.itemS != null && mplr.itemS.type != ItemID.None)
                    {
                        var tex = TextureAssets.Item[mplr.itemS.type].Value;
                        spriteBatch.Draw(tex, pos + new Vector2(8 - MathF.Sin(Main.GlobalTimeWrappedHourly * 2) * 32, -64 + MathF.Cos(Main.GlobalTimeWrappedHourly) * 16), null, Main.DiscoColor with { A = 0 }, 0, tex.Size() * .5f, (1f - ds) * (1 + fs), 0, 0);
                        spriteBatch.Draw(tex, pos + new Vector2(8 - MathF.Sin(Main.GlobalTimeWrappedHourly * 2) * 32, -64 + MathF.Cos(Main.GlobalTimeWrappedHourly) * 16), null, Color.White, 0, tex.Size() * .5f, 1f - ds, 0, 0);
                    }
                    if (mplr.itemD != null && mplr.itemD.type != ItemID.None)
                    {
                        var tex = TextureAssets.Item[mplr.itemD.type].Value;
                        spriteBatch.Draw(tex, pos + new Vector2(8 + MathF.Sin(Main.GlobalTimeWrappedHourly * 2) * 32, -64 + MathF.Cos(Main.GlobalTimeWrappedHourly) * 16), null, Main.DiscoColor with { A = 0 }, 0, tex.Size() * .5f, (1f + ds) * (1 + fs), SpriteEffects.FlipHorizontally, 0);
                        spriteBatch.Draw(tex, pos + new Vector2(8 + MathF.Sin(Main.GlobalTimeWrappedHourly * 2) * 32, -64 + MathF.Cos(Main.GlobalTimeWrappedHourly) * 16), null, Color.White, 0, tex.Size() * .5f, 1f + ds, SpriteEffects.FlipHorizontally, 0);
                    }
                    if (ds < 0 && mplr.itemS != null && mplr.itemS.type != ItemID.None)
                    {
                        var tex = TextureAssets.Item[mplr.itemS.type].Value;
                        spriteBatch.Draw(tex, pos + new Vector2(8 - MathF.Sin(Main.GlobalTimeWrappedHourly * 2) * 32, -64 + MathF.Cos(Main.GlobalTimeWrappedHourly) * 16), null, Main.DiscoColor with { A = 0 }, 0, tex.Size() * .5f, (1f - ds) * (1 + fs), 0, 0);
                        spriteBatch.Draw(tex, pos + new Vector2(8 - MathF.Sin(Main.GlobalTimeWrappedHourly * 2) * 32, -64 + MathF.Cos(Main.GlobalTimeWrappedHourly) * 16), null, Color.White, 0, tex.Size() * .5f, 1f - ds, 0, 0);
                    }
                }

            }
            return true;
        }
    }


}
