using Humanizer;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.ModLoader.Default;
using Terraria.GameContent.UI.Chat;
using Terraria.UI.Chat;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.IO;
using System.IO;
using Terraria.Localization;
using MonoMod.Cil;
using static System.Net.Mime.MediaTypeNames;
using Terraria.DataStructures;

namespace WeaponDamageInherit.Content
{
    public class WIGlobalItem : GlobalItem
    {
        public override bool InstancePerEntity => true;
        public Item targetItemClone;
        public bool UseDefaultModify(Item item)
        {
            return item.type == ItemID.LastPrism || item.DamageType == DamageClass.Summon;
        }
        public bool CheckUnavailable(Item item)
        => targetItemClone == null
        || targetItemClone.useTime == 0
        || targetItemClone.damage == 0
        || ((item.shoot == ProjectileID.None ^ targetItemClone.shoot == ProjectileID.None) && (WDIConfig.Instance.inheritCheck == WDIConfig.InheritCheckMode.EqualNoProj || WDIConfig.Instance.inheritCheck == WDIConfig.InheritCheckMode.CompatibleNoProj))
        || targetItemClone.consumable
        || targetItemClone.type == ItemID.None
        || targetItemClone.type == ModContent.ItemType<UnloadedItem>();
        public bool CheckDamageTypeInequal(DamageClass c1, DamageClass c2) => WDIConfig.Instance.DismodifyWhenUnqualified && !WeaponInheritUI.IsDamageTypeRelated(c1, c2);
        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            if (CheckUnavailable(item))//|| UseDefaultModify(item)
                return;
            if (CheckDamageTypeInequal(item.DamageType, targetItemClone.DamageType))
                return;
            var config = WDIConfig.Instance;
            var i = targetItemClone;
            float scaler = 1f;
            var mode = (byte)config.inheritCoefficient;
            if (mode % 2 == 1)
            {
                scaler *= item.useAnimation / (float)targetItemClone.useAnimation;
            }
            if (mode / 2 == 1 && (!config.sizeInfluenceMeleeCheck || ((item.DamageType.GetEffectInheritance(DamageClass.Melee) || item.DamageType == DamageClass.Melee) && (!item.noMelee || !item.noUseGraphic))))
                scaler *= (i.scale * TextureAssets.Item[i.type].Size().Length()) / (item.scale * TextureAssets.Item[item.type].Size().Length());
            if (config.useDamageLimit)
                scaler = MathHelper.Clamp(scaler, config.DamageMinLimit, config.DamageMaxLimit);
            damage *= scaler * i.damage / item.OriginalDamage;
            
            base.ModifyWeaponDamage(item, player, ref damage);
        }
        public override bool CanShoot(Item item, Player player)
        {
            return true;
            if (CheckUnavailable(item) || !UseDefaultModify(item))
                return true;

            if (CheckDamageTypeInequal(item.DamageType, targetItemClone.DamageType))
                return true;
            var config = WDIConfig.Instance;

            var i = targetItemClone.Clone();
            i.Prefix(item.prefix);
            float scaler = 1f;
            var mode = (byte)config.inheritCoefficient;
            if (mode % 2 == 1)
            {
                scaler *= item.useAnimation / (float)targetItemClone.useAnimation;
            }
            if (mode / 2 == 1 && (!config.sizeInfluenceMeleeCheck || ((item.DamageType.GetEffectInheritance(DamageClass.Melee) || item.DamageType == DamageClass.Melee) && (!item.noMelee || !item.noUseGraphic))))
                scaler *= (i.scale * TextureAssets.Item[i.type].Size().Length()) / (item.scale * TextureAssets.Item[item.type].Size().Length());
            if (config.useDamageLimit)
                scaler = MathHelper.Clamp(scaler, config.DamageMinLimit, config.DamageMaxLimit);
            item.damage = (int)(targetItemClone.damage * scaler * i.damage / item.damage);
            i = item.Clone();
            i.Prefix(item.prefix);
            item.damage = i.damage;
            return true;
        }
        public override void SaveData(Item item, TagCompound tag)
        {
            tag["targetItemClone"] = targetItemClone;
            base.SaveData(item, tag);
        }
        public override void LoadData(Item item, TagCompound tag)
        {
            targetItemClone = tag.Get<Item>("targetItemClone") ?? new Item();
            base.LoadData(item, tag);
        }
        public override void NetSend(Item item, BinaryWriter writer)
        {
            writer.Write(targetItemClone != null);
            if (targetItemClone == null) return;
            writer.Write(ItemIO.ToBase64(targetItemClone));
            base.NetSend(item, writer);
        }
        public override void NetReceive(Item item, BinaryReader reader)
        {
            bool flag = reader.ReadBoolean();
            if (flag)
            {
                targetItemClone = ItemIO.FromBase64(reader.ReadString());
            }
            base.NetReceive(item, reader);
        }
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (targetItemClone != null && targetItemClone.ModItem is UnloadedItem unloadedItem)
            {
                string tooltipText;
                if (Language.ActiveCulture.Name == "zh-Hans")
                {
                    tooltipText = $"嵌合已取消！嵌入武器{unloadedItem.ItemName}所在模组{unloadedItem.ModName}未加载，目前采用原始面板";
                }
                else
                {
                    tooltipText = $"Merge canceled! The embedded weapon {unloadedItem.ItemName} from mod {unloadedItem.ModName} is not loaded, using original stats.";
                }
                TooltipLine unloaded = new TooltipLine(Mod, "UnloadedSourceItem", tooltipText);
                tooltips.Add(unloaded);
                return;
            }
            if (targetItemClone != null && targetItemClone.consumable)
            {
                string tooltipText;
                if (Language.ActiveCulture.Name == "zh-Hans")
                {
                    tooltipText = $"{targetItemClone.Name}[i:{targetItemClone.type}]是消耗品，面板已被强制复原！";
                }
                else
                {
                    tooltipText = $"{targetItemClone.Name}[i:{targetItemClone.type}] is consumable, stats have been reset!";
                }
                TooltipLine consumable = new TooltipLine(Mod, "UnloadedSourceItem", tooltipText);
                tooltips.Add(consumable);
                return;
            }
            if (targetItemClone != null && targetItemClone.type == ItemID.None)
            {
                string tooltipText;
                if (Language.ActiveCulture.Name == "zh-Hans")
                {
                    tooltipText = $"不是，你是怎么把空气安到武器上的？";
                }
                else
                {
                    tooltipText = "How did you embed nothingness into this weapon?";
                }
                TooltipLine None = new TooltipLine(Mod, "UnloadedSourceItem", tooltipText);
                tooltips.Add(None);
                return;
            }
            if (targetItemClone == null || targetItemClone.useTime == 0 || targetItemClone.damage == 0)
                return;
            if (CheckDamageTypeInequal(item.DamageType, targetItemClone.DamageType))
            {
                string tooltipText;
                if (Language.ActiveCulture.Name == "zh-Hans")
                {
                    tooltipText = $"嵌入的武器{targetItemClone.Name}[i:{targetItemClone.type}]因继承规则变动而嵌入失败，目前采用原始面板";
                }
                else
                {
                    tooltipText = $"Embed failed: {targetItemClone.Name}[i:{targetItemClone.type}] has conflicting damage type, using original stats.";
                }
                TooltipLine disqualified = new TooltipLine(Mod, "UnloadedSourceItem", tooltipText);
                tooltips.Add(disqualified);
                return;
            }
            if (WDIConfig.Instance.inheritCheck == WDIConfig.InheritCheckMode.EqualNoProj || WDIConfig.Instance.inheritCheck == WDIConfig.InheritCheckMode.CompatibleNoProj)
            {
                bool shootConflict = (item.shoot == ProjectileID.None) ^ (targetItemClone.shoot == ProjectileID.None);
                if (shootConflict)
                {
                    string tooltipText = Language.ActiveCulture.Name == "zh-Hans"
                        ? $"嵌入的武器{targetItemClone.Name}[i:{targetItemClone.type}]因继承规则变动而嵌入失败，目前采用原始面板"
                        : $"Embed failed: {targetItemClone.Name}[i:{targetItemClone.type}] has conflicting damage type, using original stats.";

                    tooltips.Add(new TooltipLine(Mod, "UnloadedSourceItem", tooltipText));
                    return;
                }
            }
            string tooltipTextAll;
            if (Language.ActiveCulture.Name == "zh-Hans")
            {
                tooltipTextAll = $"本武器伤害面板继承自{targetItemClone.Name}[i:{targetItemClone.type}](具体算法见模组简介)";
            }
            else
            {
                tooltipTextAll = $"Stats inherited from {targetItemClone.Name}[i:{targetItemClone.type}] (see mod description for details)";
            }
            TooltipLine newLine = new TooltipLine(Mod, "UnloadedSourceItem", tooltipTextAll);
            tooltips.Add(newLine);
            base.ModifyTooltips(item, tooltips);
        }
    }
    public class WIGlobalProjForSummon : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (source is EntitySource_ItemUse_WithAmmo itemUse_WithAmmo)
                sourceItemClone = itemUse_WithAmmo.Item;

            base.OnSpawn(projectile, source);
        }
        public Item sourceItemClone;
    }
    public class WeaponInheritSystem : ModSystem
    {
        public override void Load()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                instance = this;
                weaponInheritUI = new WeaponInheritUI();
                userInterface = new UserInterface();
                weaponInheritUI.Activate();
                userInterface.SetState(weaponInheritUI);
            }
            IL_Projectile.Update += WeaponInheritModifyForSummon;
        }

        private void WeaponInheritModifyForSummon(MonoMod.Cil.ILContext il)
        {
            var ilCursor = new ILCursor(il);
            if (!ilCursor.TryGotoNext(i => i.MatchLdfld(typeof(Projectile), "originalDamage")))
                return;
            for (int n = 0; n < 3; n++)
                if (!ilCursor.TryGotoPrev(i => i.MatchLdarg0()))
                    return;
            ilCursor.RemoveRange(13);

            ilCursor.EmitLdarg0();
            ilCursor.EmitDelegate<Action<Projectile>>(proj =>
            {
                Player player = Main.player[proj.owner];
                StatModifier modifier = player.GetTotalDamage(proj.DamageType);
                if (proj.TryGetGlobalProjectile<WIGlobalProjForSummon>(out var globalProj) && globalProj.sourceItemClone != null)
                    CombinedHooks.ModifyWeaponDamage(player, globalProj.sourceItemClone, ref modifier);
                proj.damage = (int)modifier.ApplyTo(proj.originalDamage);
            });
        }

        public static WeaponInheritSystem instance;
        public WeaponInheritUI weaponInheritUI;
        public UserInterface userInterface;
        public override void UpdateUI(GameTime gameTime)
        {
            if (WeaponInheritUI.Visible)
            {
                userInterface?.Update(gameTime);
            }
            base.UpdateUI(gameTime);
        }
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            //寻找一个名字为Vanilla: Mouse Text的绘制层，也就是绘制鼠标字体的那一层，并且返回那一层的索引
            int MouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            //寻找到索引时
            if (MouseTextIndex != -1)
            {
                //往绘制层集合插入一个成员，第一个参数是插入的地方的索引，第二个参数是绘制层
                layers.Insert(MouseTextIndex, new LegacyGameInterfaceLayer(
                   //这里是绘制层的名字
                   "WeaponDamageInherit:WeaponInheritmUI",
                   //这里是匿名方法
                   delegate
                   {
                       //当Visible开启时（当UI开启时）
                       if (WeaponInheritUI.Visible)
                           //绘制UI（运行exampleUI的Draw方法）
                           weaponInheritUI.Draw(Main.spriteBatch);
                       return true;
                   },
                   //这里是绘制层的类型
                   InterfaceScaleType.UI)
               );
            }
            base.ModifyInterfaceLayers(layers);
        }

    }

    /// <summary>
    /// 一个仿原版制作的物品UI格，由于是单独的所以应该适配|  来自QOT
    /// </summary>
    public class ModItemSlot : UIElement
    {
        /// <summary>
        /// 无物品时显示的贴图
        /// </summary>
        private readonly Asset<Texture2D> _emptyTexture;
        public float emptyTextureScale = 1f;
        public float emptyTextureOpacity = 0.5f;
        private readonly Func<string> _emptyText; // 无物品的悬停文本

        public Item Item;
        public float Scale = 1f;

        /// <summary>
        /// 是否使用基于Shader的圆润边框
        /// </summary>
        public bool RoundBorder = true;
        /// <summary>
        /// 是否可交互，否则不能执行左右键操作
        /// </summary>
        public bool Interactable = true;
        /// <summary>
        /// 该槽位内的饰品/装备是否可在被右键时自动装备
        /// </summary>
        public bool AllowSwapEquip;
        /// <summary>
        /// 该槽位内的物品可否Alt键收藏
        /// </summary>
        public bool AllowFavorite;

        /// <summary>
        /// 物品槽UI元件
        /// </summary>
        /// <param name="scale">物品在槽内显示的大小，0.85是游戏内物品栏的大小</param>
        /// <param name="emptyTexturePath">当槽内无物品时，显示的贴图</param>
        /// <param name="emptyText">当槽内无物品时，悬停显示的文本</param>
        public ModItemSlot(float scale = 0.85f, string emptyTexturePath = null, Func<string> emptyText = null)
        {
            this.Width.Set(52, 0);
            this.Height.Set(52, 0);
            Item = new Item();
            Item.SetDefaults();
            Scale = scale;
            AllowSwapEquip = false;
            AllowFavorite = true;
            if (emptyTexturePath is not null && ModContent.HasAsset(emptyTexturePath))
            {
                _emptyTexture = ModContent.Request<Texture2D>(emptyTexturePath);
            }
            _emptyText = emptyText;
        }

        /// <summary>
        /// 改原版的<see cref="Main.cursorOverride"/>
        /// </summary>
        private void SetCursorOverride()
        {
            if (!Item.IsAir)
            {
                if (!Item.favorited && ItemSlot.ShiftInUse)
                {
                    Main.cursorOverride = CursorOverrideID.ChestToInventory; // 快捷放回物品栏图标
                }
                if (Main.keyState.IsKeyDown(Main.FavoriteKey))
                {
                    if (AllowFavorite)
                    {
                        Main.cursorOverride = CursorOverrideID.FavoriteStar; // 收藏图标
                    }
                    if (Main.drawingPlayerChat)
                    {
                        Main.cursorOverride = CursorOverrideID.Magnifiers; // 放大镜图标 - 输入到聊天框
                    }
                }
                void TryTrashCursorOverride()
                {
                    if (!Item.favorited)
                    {
                        if (Main.npcShop > 0)
                        {
                            Main.cursorOverride = CursorOverrideID.QuickSell; // 卖出图标
                        }
                        else
                        {
                            Main.cursorOverride = CursorOverrideID.TrashCan; // 垃圾箱图标
                        }
                    }
                }
                if (ItemSlot.ControlInUse && ItemSlot.Options.DisableLeftShiftTrashCan && !ItemSlot.ShiftForcedOn)
                {
                    TryTrashCursorOverride();
                }
                // 如果左Shift快速丢弃打开了，按原版物品栏的物品应该是丢弃，但是我们这应该算箱子物品，所以不丢弃
                //if (!ItemSlot.Options.DisableLeftShiftTrashCan && ItemSlot.ShiftInUse) {
                //    TryTrashCursorOverride();
                //}
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (Item is null)
            {
                Item = new Item(0);
                ItemChange();
            }

            // 在Panel外的也有IsMouseHovering
            var dimensions = GetDimensions();
            bool isMouseHovering = dimensions.ToRectangle().Contains(Main.MouseScreen.ToPoint());

            int lastStack = Item.stack;
            int lastType = Item.type;
            // 我把右键长按放在这里执行了
            if (isMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                if (Interactable)
                {
                    SetCursorOverride();
                    // 伪装，然后进行原版右键尝试
                    // 千万不要伪装成箱子，因为那样多人会传同步信息，然后理所当然得出Bug
                    if (Item is not null && !Item.IsAir)
                    {
                        ItemSlot.RightClick(ref Item, AllowSwapEquip ? ItemSlot.Context.InventoryItem : ItemSlot.Context.CreativeSacrifice);
                    }
                }
                DrawText();
            }
            if (lastStack != Item.stack || lastType != Item.type)
            {
                ItemChange(true);
                RightClickItemChange(Item.stack - lastStack, lastType != Item.type);
                Main.playerInventory = true;
            }

            Vector2 origin = GetDimensions().Position();

            //if (RoundBorder)
            //{
            //    var borderColor = Item.favorited ? UIStyle.ItemSlotBorderFav : UIStyle.ItemSlotBorder;
            //    var background = Item.favorited ? UIStyle.ItemSlotBgFav : UIStyle.ItemSlotBg;
            //    SDFRectangle.HasBorder(dimensions.Position(), dimensions.Size(),
            //        new Vector4(UIStyle.ItemSlotBorderRound), background, UIStyle.ItemSlotBorderSize, borderColor);
            //}

            // 这里设置inventoryScale原版也是这么干的
            float oldScale = Main.inventoryScale;
            Main.inventoryScale = Scale;

            // 假装自己是一个物品栏物品拿去绘制
            var temp = new Item[11];
            // 如果用圆润边框，就假装为ChatItem，不会绘制原版边框
            int context = RoundBorder ? ItemSlot.Context.ChatItem : ItemSlot.Context.InventoryItem;
            temp[10] = Item;
            ItemSlot.Draw(Main.spriteBatch, temp, context, 10, origin);

            Main.inventoryScale = oldScale;

            // 空物品的话显示空贴图
            if (Item.IsAir)
            {
                if (_emptyText is not null && isMouseHovering && Main.mouseItem.IsAir)
                {
                    Main.instance.MouseText(_emptyText.Invoke());
                }
                if (_emptyTexture is not null)
                {
                    origin = _emptyTexture.Size() / 2f;
                    spriteBatch.Draw(_emptyTexture.Value, GetDimensions().Center(), null, Color.White * emptyTextureOpacity, 0f, origin, emptyTextureScale, SpriteEffects.None, 0f);
                }
            }
        }

        /// <summary>
        /// 修改MouseText的
        /// </summary>
        public void DrawText()
        {
            if (!Item.IsAir && (Main.mouseItem is null || Main.mouseItem.IsAir))
            {
                Main.HoverItem = Item.Clone();
                Main.instance.MouseText(string.Empty);
            }
        }
        /// <summary>
        /// 强制性的可否放置判断，只有满足条件才能放置物品，没有例外|  来自QOT
        /// </summary>
        /// <param name="slotItem">槽内物品</param>
        /// <param name="mouseItem">手持物品</param>
        /// <returns>
        /// 强制判断返回值，判断放物类型
        /// 0: 不可放物<br/>
        /// 1: 两物品不同，应该切换<br/>
        /// 2: 两物品相同，应该堆叠<br/>
        /// 3: 槽内物品为空，应该切换<br/>
        /// </returns>
        public static byte CanPlaceInSlot(Item slotItem, Item mouseItem)
        {
            if (slotItem.IsAir)
                return 3;
            if (mouseItem.type != slotItem.type || mouseItem.prefix != slotItem.prefix)
                return 1;
            if (!slotItem.IsAir && slotItem.stack < slotItem.maxStack && ItemLoader.CanStack(slotItem, mouseItem))
                return 2;
            return 0;
        }
        public void LeftClickItem(ref Item placeItem)
        {
            // 放大镜图标 - 输入到聊天框
            if (Main.cursorOverride == CursorOverrideID.Magnifiers)
            {
                if (ChatManager.AddChatText(FontAssets.MouseText.Value, ItemTagHandler.GenerateTag(Item), Vector2.One))
                    SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }

            // 收藏图标
            if (Main.cursorOverride == CursorOverrideID.FavoriteStar)
            {
                Item.favorited = !Item.favorited;
                SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }

            // 垃圾箱图标
            if (Main.cursorOverride == CursorOverrideID.TrashCan)
            {
                // 假装自己是一个物品栏物品
                var temp = new Item[1];
                temp[0] = Item;
                ItemSlot.SellOrTrash(temp, ItemSlot.Context.InventoryItem, 0);
                return;
            }

            // 放回物品栏图标
            if (Main.cursorOverride == CursorOverrideID.ChestToInventory)
            {
                int oldStack = Item.stack;
                Item = Main.player[Main.myPlayer].GetItem(Main.myPlayer, Item, GetItemSettings.InventoryEntityToPlayerInventorySettings);
                if (Item.stack != oldStack) // 成功了
                {
                    if (Item.stack <= 0)
                        Item.SetDefaults();
                    SoundEngine.PlaySound(SoundID.Grab);
                }
                return;
            }

            if (Main.mouseItem.IsAir && Item.IsAir) return;

            // 常规单点
            if (placeItem is not null && CanPlaceItem(placeItem))
            {
                byte placeMode = CanPlaceInSlot(Item, placeItem);

                // type不同直接切换吧
                if (placeMode is 1 or 3)
                {
                    SwapItem(ref placeItem);
                    SoundEngine.PlaySound(SoundID.Grab);
                    Main.playerInventory = true;
                    return;
                }
                // type相同，里面的能堆叠，放进去
                if (placeMode is 2)
                {
                    ItemLoader.TryStackItems(Item, placeItem, out _);
                    SoundEngine.PlaySound(SoundID.Grab);
                }
            }
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);

            if (!Main.LocalPlayer.ItemTimeIsZero || Main.LocalPlayer.itemAnimation != 0 || !Interactable)
            {
                return;
            }

            if (Item is null)
            {
                Item = new Item();
                Item.SetDefaults();
                ItemChange();
            }

            int lastStack = Item.stack;
            int lastType = Item.type;
            int lastPrefix = Item.prefix;

            SetCursorOverride(); // Click在Update执行，因此必须在这里设置一次
            LeftClickItem(ref Main.mouseItem);

            if (lastStack != Item.stack || lastType != Item.type || lastPrefix != Item.prefix)
            {
                ItemChange();
            }
        }

        /// <summary>
        /// 可以在这里写额外的物品放置判定，第一个Item是当前槽位存储物品，第二个Item是<see cref="Main.mouseItem"/>
        /// </summary>
        public Func<Item, Item, bool> OnCanPlaceItem;
        public bool CanPlaceItem(Item item)
        {
            bool canPlace = true;

            if (Item is null)
            {
                Item = new Item();
                Item.SetDefaults();
            }

            if (OnCanPlaceItem is not null)
            {
                canPlace = OnCanPlaceItem.Invoke(Item, item);
            }

            return canPlace;
        }

        /// <summary>
        /// 物品改变后执行，可以写保存之类的
        /// </summary>
        public Action<Item, bool> OnItemChange;
        public virtual void ItemChange(bool rightClick = false)
        {
            if (Item is null)
            {
                Item = new Item();
                Item.SetDefaults();
            }

            if (OnItemChange is not null)
            {
                OnItemChange.Invoke(Item, rightClick);
            }
        }

        /// <summary>
        /// 右键物品改变了才执行
        /// </summary>
        public Action<Item, int, bool> OnRightClickItemChange;
        public virtual void RightClickItemChange(int stackChange, bool typeChange)
        {
            if (Item is null)
            {
                Item = new Item();
                Item.SetDefaults();
            }

            if (OnRightClickItemChange is not null)
            {
                OnRightClickItemChange.Invoke(Item, stackChange, typeChange);
            }
        }

        public void SwapItem(ref Item item)
        {
            Utils.Swap(ref item, ref Item);
        }

        public void Unload()
        {
            Item = null;
        }
    }
    public class DraggablePanel : UIPanel
    {
        public bool Dragging;
        public Vector2 mousePos;
        public Vector2 offset;
        public Vector2 currentOffset;
        public override void LeftMouseDown(UIMouseEvent evt)
        {
            if (evt.Target != this) return;
            Dragging = true;
            mousePos = evt.MousePosition;
            base.LeftMouseDown(evt);
        }
        public override void LeftMouseUp(UIMouseEvent evt)
        {
            if (evt.Target != this) return;
            Dragging = false;
            offset += currentOffset;
            currentOffset = default;
            base.LeftMouseUp(evt);
        }
        public override void Update(GameTime gameTime)
        {
            //if (Dragging)
            //{
            //    Left.Set(Main.mouseX - offset.X, 0f);
            //    Top.Set(Main.mouseY - offset.Y, 0f);
            //    Recalculate();
            //}
            if (Dragging)
            {
                currentOffset = Main.MouseScreen - mousePos;
            }
            base.Update(gameTime);
        }
    }
    public class WeaponInheritAssitPlayer : ModPlayer
    {
        public Item itemD = new Item();
        public Item itemS = new Item();
        public Item itemR = new Item();
        public override void SaveData(TagCompound tag)
        {
            tag["itemD"] = itemD;
            tag["itemS"] = itemS;
            tag["itemR"] = itemR;
            base.SaveData(tag);
        }
        public override void LoadData(TagCompound tag)
        {
            itemD = tag.Get<Item>("itemD") ?? new Item();
            itemS = tag.Get<Item>("itemS") ?? new Item();
            itemR = tag.Get<Item>("itemR") ?? new Item();

            base.LoadData(tag);
        }
        public Point? lastChoseTileCoord;
    }
    public class WeaponInheritUI : UIState
    {
        public static bool Visible;
        //public static int Timer;
        public override void Update(GameTime gameTime)
        {
            var coord = Main.LocalPlayer.GetModPlayer<WeaponInheritAssitPlayer>().lastChoseTileCoord;
            if (coord.HasValue)
            {
                var vec = new Vector2(coord.Value.X - MathHelper.Lerp(4, 6, Utils.GetLerpValue(0.5f, 2f, Main.UIScale)) * Main.UIScale, coord.Value.Y - 6 * Main.UIScale) * 16 - Main.screenPosition;
                vec /= Main.UIScale;
                var m = Main.MouseScreen;
                float zoom = Main.GameZoomTarget * Main.ForcedMinimumZoom;
                vec = (vec - Main.ScreenSize.ToVector2() * .5f) * zoom + Main.ScreenSize.ToVector2() * .5f;

                //vec *= Main.UIScale;
                panel.Top.Set(vec.Y + panel.offset.Y + panel.currentOffset.Y, 0);  //
                panel.Left.Set(vec.X + panel.offset.X + panel.currentOffset.X, 0);  //
                Recalculate();

                if (Vector2.Distance(Main.LocalPlayer.Center, coord.Value.ToVector2() * 16) > 256)
                    Close();
            }
            //Timer += Visible ? 1 : -1;
            //Timer = Math.Clamp(Timer, 0, 10);
            //panel.Height.Set(100 * Timer / 10f, 0);
            base.Update(gameTime);
        }
        public ModItemSlot slotDestination;
        public ModItemSlot slotSource;
        public ModItemSlot slotResult;
        public DraggablePanel panel;
        static bool EqualOrInherit(DamageClass class1, DamageClass class2) => class1 == class2 || class1.GetEffectInheritance(class2);
        public static bool IsDamageTypeRelated(DamageClass class1, DamageClass class2)
        {
            switch (WDIConfig.Instance.inheritCheck)
            {
                case WDIConfig.InheritCheckMode.All: return true;
                case WDIConfig.InheritCheckMode.Equal: return class1 == class2;
                case WDIConfig.InheritCheckMode.EqualNoProj: return class1 == class2;
                case WDIConfig.InheritCheckMode.ExtendedFamily:
                    {
                        if (EqualOrInherit(class1, DamageClass.SummonMeleeSpeed) && EqualOrInherit(class2, DamageClass.Summon)) return false;
                        if (EqualOrInherit(class2, DamageClass.SummonMeleeSpeed) && EqualOrInherit(class1, DamageClass.Summon)) return false;

                        if (class1 is not VanillaDamageClass || class2 is not VanillaDamageClass)
                            return true;
                        DamageClass[] types = [DamageClass.Melee, DamageClass.Magic, DamageClass.Ranged, DamageClass.Summon, DamageClass.SummonMeleeSpeed, DamageClass.Throwing];
                        foreach (var type in types)
                        {
                            if (EqualOrInherit(class1, type) && EqualOrInherit(class2, type))
                                return true;
                        }
                        return false;
                    }
                case WDIConfig.InheritCheckMode.Compatible:
                    {
                        if (class1 is not VanillaDamageClass || class2 is not VanillaDamageClass)
                            return true;
                        return class1 == class2;
                    }
                case WDIConfig.InheritCheckMode.CompatibleNoProj:
                    {
                        if (class1 is not VanillaDamageClass || class2 is not VanillaDamageClass)
                            return true;
                        return class1 == class2;
                    }
                case WDIConfig.InheritCheckMode.CoreFamily:
                    {
                        if (class1 == DamageClass.SummonMeleeSpeed && class2 == DamageClass.Summon) return false;
                        if (class1 == DamageClass.Summon && class2 == DamageClass.SummonMeleeSpeed) return false;

                        return class1 == class2 || class1.GetEffectInheritance(class2) || class2.GetEffectInheritance(class1);
                    }
                default:
                    return false;
            }
        }
        public override void OnInitialize()
        {
            panel = new DraggablePanel();
            panel.Width.Set(280, 0f);
            panel.Height.Set(100, 0);
            Append(panel);
            slotDestination = new ModItemSlot(0.85f, "WeaponDamageInherit/Content/Infinite_Icons_Weapon", () =>
                Language.ActiveCulture.Name == "zh-Hans" ? "请放入 目标武器" : "Target Weapon");

            slotSource = new ModItemSlot(0.85f, "WeaponDamageInherit/Content/Infinite_Icons_Weapon", () =>
                Language.ActiveCulture.Name == "zh-Hans" ? "请放入 面板武器" : "Stat Weapon");

            slotResult = new ModItemSlot(0.85f, "WeaponDamageInherit/Content/Infinite_Icons_Weapon", () =>
                Language.ActiveCulture.Name == "zh-Hans" ? "合成结果" : "Result Weapon");
            slotDestination.OnItemChange += (item, flag) =>
            {
                var mplr = Main.LocalPlayer?.GetModPlayer<WeaponInheritAssitPlayer>();
                if (mplr != null)
                    mplr.itemD = slotDestination.Item;
            };
            slotDestination.OnUpdate += _ =>
            {
                var mplr = Main.LocalPlayer?.GetModPlayer<WeaponInheritAssitPlayer>();
                if (mplr != null)
                    slotDestination.Item = mplr.itemD;
            };
            slotSource.OnItemChange += (item, flag) =>
            {
                var mplr = Main.LocalPlayer?.GetModPlayer<WeaponInheritAssitPlayer>();
                if (mplr != null)
                    mplr.itemS = slotSource.Item;
            };
            slotSource.OnUpdate += _ =>
            {
                var mplr = Main.LocalPlayer?.GetModPlayer<WeaponInheritAssitPlayer>();
                if (mplr != null)
                    slotSource.Item = mplr.itemS;
            };
            slotResult.OnItemChange += (item, flag) =>
            {
                var mplr = Main.LocalPlayer?.GetModPlayer<WeaponInheritAssitPlayer>();
                if (mplr != null)
                    mplr.itemR = slotResult.Item;
            };
            slotResult.OnUpdate += _ =>
            {
                var mplr = Main.LocalPlayer?.GetModPlayer<WeaponInheritAssitPlayer>();
                if (mplr != null)
                    slotResult.Item = mplr.itemR;
            };
            slotDestination.OnCanPlaceItem += (i1, i2) =>
            {
                if (i2.type == ItemID.None) return true;
                if (i1.type == ItemID.None && i2.damage <= 0)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("请放入武器", Color.Red);
                    else Main.NewText("Please place target weapon", Color.Red);
                    return false;
                }
                if (slotSource.Item != null && slotSource.Item.type != ItemID.None && !IsDamageTypeRelated(i2.DamageType, slotSource.Item.DamageType))
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("与面板武器伤害类型不一致", Color.Red);
                    else Main.NewText("Damage type mismatch with stat weapon", Color.Red);
                    return false;
                }
                if ((WDIConfig.Instance.inheritCheck == WDIConfig.InheritCheckMode.EqualNoProj
                    || WDIConfig.Instance.inheritCheck == WDIConfig.InheritCheckMode.CompatibleNoProj)
                    && (slotSource.Item.shoot == ProjectileID.None ^ i2.shoot == ProjectileID.None) && (slotSource.Item.type != ItemID.None && i2.type != ItemID.None))
                {
                    string message = (slotSource.Item.shoot == ProjectileID.None) ?
                        (Language.ActiveCulture.Name == "zh-Hans" ?
                            "普通武器不能继承弹幕武器！" :
                            "Projectile-launching weapons and non-projectile weapons are incompatible with each other!") :
                        (Language.ActiveCulture.Name == "zh-Hans" ?
                            "弹幕武器不能继承普通武器！" :
                            "Projectile-launching weapons and non-projectile weapons are incompatible with each other!");

                    Main.NewText(message, Color.Red);
                    return false;
                }
                if (i2.ammo != AmmoID.None)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("弹药不可以！！", Color.Red);
                    else Main.NewText("Ammo not allowed!", Color.Red);
                    return false;
                }
                if (i2.consumable)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("消耗品不要！！", Color.Red);
                    else Main.NewText("Consumables not allowed!", Color.Red);
                    return false;
                }
                return true;
            };
            slotSource.OnCanPlaceItem += (i1, i2) =>
            {
                if (i2.type == ItemID.None) return true;
                if (i1.type == ItemID.None && i2.damage <= 0)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("请放入武器", Color.Red);
                    else Main.NewText("Please place target weapon", Color.Red);
                    return false;
                }
                if (slotDestination.Item != null && slotDestination.Item.type != ItemID.None && !IsDamageTypeRelated(i2.DamageType, slotDestination.Item.DamageType))
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("与面板武器伤害类型不一致", Color.Red);
                    else Main.NewText("Damage type mismatch with stat weapon", Color.Red);
                    return false;
                }
                if ((WDIConfig.Instance.inheritCheck == WDIConfig.InheritCheckMode.EqualNoProj
                    || WDIConfig.Instance.inheritCheck == WDIConfig.InheritCheckMode.CompatibleNoProj)
                    && (slotDestination.Item.shoot == ProjectileID.None ^ i2.shoot == ProjectileID.None) && (slotDestination.Item.type != ItemID.None && i2.type != ItemID.None))
                {
                    string message = (slotDestination.Item.shoot == ProjectileID.None) ?
                        (Language.ActiveCulture.Name == "zh-Hans" ?
                            "普通武器不能继承弹幕武器！" :
                            "Projectile-launching weapons and non-projectile weapons are incompatible with each other!") :
                        (Language.ActiveCulture.Name == "zh-Hans" ?
                            "弹幕武器不能继承普通武器！" :
                            "Projectile-launching weapons and non-projectile weapons are incompatible with each other!");

                    Main.NewText(message, Color.Red);
                    return false;
                }
                if (i2.ammo != AmmoID.None)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("弹药不可以！！", Color.Red);
                    else Main.NewText("Ammo not allowed!", Color.Red);
                    return false;
                }
                if (i2.consumable)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("消耗品不要！！", Color.Red);
                    else Main.NewText("Consumables not allowed!", Color.Red);
                    return false;
                }
                return true;
            };
            slotDestination.Left.Set(0, 0);
            slotSource.Left.Set(60, 0);
            slotResult.HAlign = 1;
            slotResult.OnCanPlaceItem += (i1, i2) => i2.type == ItemID.None;

            panel.Append(slotDestination);
            panel.Append(slotSource);
            panel.Append(slotResult);
            UIImageButton splitButton = new UIImageButton(ModContent.Request<Texture2D>("WeaponDamageInherit/Content/ButtonUpDown", AssetRequestMode.ImmediateLoad));
            splitButton.Top.Set(-20, 1);
            splitButton.Left.Set(-120, 1);
            splitButton.OnLeftClick += (evt, elem) =>
            {
                var itemD = slotDestination.Item;
                if (itemD == null || itemD.type == ItemID.None)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("你没本体武器我拆解个集贸啊(", Color.Red);
                    else Main.NewText("No target weapon to disassemble!", Color.Red);
                    return;
                }
                var itemC = itemD.GetGlobalItem<WIGlobalItem>().targetItemClone;
                if (itemC == null || itemC.type == ItemID.None)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("你没嵌入武器我拆解个集贸啊(", Color.Red);
                    else Main.NewText("No stat weapon found!", Color.Red);
                    return;
                }
                var itemS = slotSource.Item;
                if (itemS != null && itemS.type != ItemID.None)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("你有武器占在产品槽我能怎么办(", Color.Red);
                    else Main.NewText("Clear the result slot first!", Color.Red);
                    return;
                }
                var itemR = slotResult.Item;
                if (itemR != null && itemR.type != ItemID.None)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("你有武器占在产品槽我能怎么办(", Color.Red);
                    else Main.NewText("Clear the result slot first!", Color.Red);
                    return;
                }
                var mplr = Main.LocalPlayer.GetModPlayer<WeaponInheritAssitPlayer>();

                mplr.itemS = itemC.Clone();
                itemC.TurnToAir();
                itemD.GetGlobalItem<WIGlobalItem>().targetItemClone = null;
                mplr.itemR = itemD.Clone();
                mplr.itemD.TurnToAir();
                SoundEngine.PlaySound(SoundID.Item176, Main.LocalPlayer.Center);
                SoundEngine.PlaySound(SoundID.ResearchComplete, Main.LocalPlayer.Center);
                if (Language.ActiveCulture.Name == "zh-Hans")
                {
                    Main.NewText("拆解成功！", Color.LimeGreen);
                }
                else
                {
                    Main.NewText("Disassembly successful!", Color.LimeGreen);
                }
            };
            UIImageButton combineButton = new UIImageButton(ModContent.Request<Texture2D>("Terraria/Images/UI/ButtonPlay", AssetRequestMode.ImmediateLoad));
            combineButton.Top.Set(-20, 1);
            combineButton.Left.Set(-80, 1);
            combineButton.OnLeftClick += (evt, elem) =>
            {
                var itemD = slotDestination.Item;
                bool failed = false;
                if (itemD == null || itemD.type == ItemID.None)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("请放入目标武器", Color.Red);
                    else Main.NewText("Please place target weapon", Color.Red);
                    failed = true;
                }
                if (itemD.type == ModContent.ItemType<UnloadedItem>())
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("物品未加载", Color.Red);
                    else Main.NewText("Item not loaded", Color.Red);
                    failed = true;
                }
                else if (itemD.damage == 0 || itemD.useTime == 0 || itemD.consumable)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("我不承认目标处这玩意是武器", Color.Red);
                    else Main.NewText("Invalid target weapon", Color.Red);
                    failed = true;
                }

                var itemS = slotSource.Item;
                if (itemS == null || itemS.type == ItemID.None)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("请放入面板武器", Color.Red);
                    else Main.NewText("Please place stat weapon", Color.Red);
                    failed = true;
                }
                if (itemD.shoot == ProjectileID.None && itemS.shoot != ProjectileID.None && WDIConfig.Instance.inheritCheck == WDIConfig.InheritCheckMode.EqualNoProj)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("普通武器不能继承弹幕武器！", Color.Red);
                    else Main.NewText("Projectile-launching weapons and non-projectile weapons are incompatible with each other!", Color.Red);
                    failed = true;
                }
                if (itemD.shoot != ProjectileID.None && itemS.shoot == ProjectileID.None && WDIConfig.Instance.inheritCheck == WDIConfig.InheritCheckMode.EqualNoProj)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("弹幕武器不能继承普通武器！", Color.Red);
                    else Main.NewText("Projectile-launching weapons and non-projectile weapons are incompatible with each other!", Color.Red);
                    failed = true;
                }
                if (itemD.shoot == ProjectileID.None && itemS.shoot != ProjectileID.None && WDIConfig.Instance.inheritCheck == WDIConfig.InheritCheckMode.CompatibleNoProj)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("普通武器不能继承弹幕武器！", Color.Red);
                    else Main.NewText("Projectile-launching weapons and non-projectile weapons are incompatible with each other!", Color.Red);
                    failed = true;
                }
                if (itemD.shoot != ProjectileID.None && itemS.shoot == ProjectileID.None && WDIConfig.Instance.inheritCheck == WDIConfig.InheritCheckMode.CompatibleNoProj)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("弹幕武器不能继承普通武器！", Color.Red);
                    else Main.NewText("Projectile-launching weapons and non-projectile weapons are incompatible with each other!", Color.Red);
                    failed = true;
                }
                if (itemS.type == ModContent.ItemType<UnloadedItem>())
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("物品未加载", Color.Red);
                    else Main.NewText("Item not loaded", Color.Red);
                    failed = true;
                }
                else if (itemS.damage == 0 || itemD.useTime == 0 || itemD.consumable)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("我不承认面板处这玩意是武器", Color.Red);
                    else Main.NewText("Invalid target weapon", Color.Red);
                    failed = true;
                }
                if (!IsDamageTypeRelated(itemD.DamageType, itemS.DamageType))
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("武器伤害类型不一致", Color.Red);
                    else Main.NewText("Damage type mismatch", Color.Red);
                    Main.NewText(itemD.DamageType.GetType().FullName);
                    Main.NewText(itemS.DamageType.GetType().FullName);

                    failed = true;
                }
                if (slotResult.Item != null && slotResult.Item.type != ItemID.None)
                {
                    if (Language.ActiveCulture.Name == "zh-Hans") Main.NewText("请先拿走上次合成的武器", Color.Red);
                    else Main.NewText("Clear result slot first", Color.Red);
                    failed = true;
                }
                if (failed)
                    return;
                label:
                slotResult.Item = itemD.Clone();
                Main.LocalPlayer.GetModPlayer<WeaponInheritAssitPlayer>().itemR = slotResult.Item;
                Item cloneItem = null;
                if (slotResult.Item.GetGlobalItem<WIGlobalItem>().targetItemClone != null)
                {
                    cloneItem = slotResult.Item.GetGlobalItem<WIGlobalItem>().targetItemClone.Clone();
                }
                slotResult.Item.GetGlobalItem<WIGlobalItem>().targetItemClone = itemS.Clone();
                if (cloneItem != null)
                {
                    slotSource.Item = cloneItem;
                    Main.LocalPlayer.GetModPlayer<WeaponInheritAssitPlayer>().itemS = slotSource.Item;

                }
                else
                {
                    slotSource.Item.TurnToAir();
                }
                itemD.TurnToAir();
                SoundEngine.PlaySound(SoundID.Item176, Main.LocalPlayer.Center);
                SoundEngine.PlaySound(SoundID.ResearchComplete, Main.LocalPlayer.Center);
                if (Language.ActiveCulture.Name == "zh-Hans")
                {
                    Main.NewText("继承成功！", Color.LimeGreen);
                }
                else
                {
                    Main.NewText("Inheritance successful!", Color.LimeGreen);
                }
            };
            UIImageButton closeButton = new UIImageButton(ModContent.Request<Texture2D>("WeaponDamageInherit/Content/ButtonClose", AssetRequestMode.ImmediateLoad));
            closeButton.Top.Set(-20, 1);
            closeButton.Left.Set(-40, 1);
            closeButton.OnLeftClick += (evt, elem) =>
            {
                Close();
            };
            panel.Append(splitButton);
            panel.Append(combineButton);
            panel.Append(closeButton);
            base.OnInitialize();
        }
        public void Open()
        {
            SoundEngine.PlaySound(SoundID.MenuOpen);
            Visible = true;
            panel.Width.Set(280, 0);
        }
        public void Close()
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            panel.offset = panel.mousePos = panel.currentOffset = default;
            Visible = false;
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            var arrow = ModContent.Request<Texture2D>("WeaponDamageInherit/Content/Misc_4").Value;
            var cen = panel.GetDimensions().Center();
            spriteBatch.Draw(arrow, cen + new Vector2(32, -16) + Main.rand.NextVector2Unit() * 2, null, Main.DiscoColor with { A = 0 } * .25f, 0, new Vector2(78, 21) * .5f, 1.25f, 0, 0);
            spriteBatch.Draw(arrow, cen + new Vector2(32, -16), null, Color.White with { A = 0 } * .25f, 0, new Vector2(78, 21) * .5f, 1.15f, 0, 0);
            spriteBatch.Draw(arrow, cen + new Vector2(32, -16), null, Color.White with { A = 0 } * .75f, 0, new Vector2(78, 21) * .5f, 1f, 0, 0);

            var cross = ModContent.Request<Texture2D>("WeaponDamageInherit/Content/Misc_5").Value;
            spriteBatch.Draw(cross, cen + new Vector2(-72, -16), new Rectangle(0, 0, 32, 32), Color.White with { A = 0 } * .75f, MathHelper.PiOver4, new Vector2(16), .5f, 0, 0);
            spriteBatch.Draw(cross, cen + new Vector2(-72, -16), new Rectangle(0, 0, 32, 32), Color.White with { A = 0 } * .25f, MathHelper.PiOver4, new Vector2(16), .5f * 1.15f, 0, 0);
            spriteBatch.Draw(cross, cen + new Vector2(-72, -16) + Main.rand.NextVector2Unit(), new Rectangle(0, 0, 32, 32), Main.DiscoColor with { A = 0 } * .25f, MathHelper.PiOver4, new Vector2(16), .5f * 1.25f, 0, 0);


        }
    }
}
