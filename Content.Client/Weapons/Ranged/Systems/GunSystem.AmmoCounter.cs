// <Trauma>
using System.Linq;
// </Trauma>
using System.Numerics;
using Content.Client.IoC;
using Content.Client.Items;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Weapons.Ranged.Components;
using Content.Client.Weapons.Ranged.ItemStatus;
using Robust.Client.Animations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    private void OnAmmoCounterCollect(EntityUid uid, AmmoCounterComponent component, ItemStatusCollectMessage args)
    {
        RefreshControl(uid, component);

        if (component.Control != null)
            args.Controls.Add(component.Control);
    }

    /// <summary>
    /// Refreshes the control being used to show ammo. Useful if you change the AmmoProvider.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    private void RefreshControl(EntityUid uid, AmmoCounterComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        component.Control?.Dispose();
        component.Control = null;

        var ev = new AmmoCounterControlEvent();
        RaiseLocalEvent(uid, ev, false);

        // Fallback to default if none specified
        ev.Control ??= new DefaultStatusControl();

        // <Trauma>
        if (ev.Controls.Count > 1)
        {
            var container = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                VerticalExpand = true,
            };
            foreach (var control in ev.Controls)
            {
                container.AddChild(control);
            }

            component.Control = container;
            UpdateAmmoCount(uid, component);
            return;
        }
        // </Trauma>

        component.Control = ev.Control;
        UpdateAmmoCount(uid, component);
    }

    private void UpdateAmmoCount(EntityUid uid, AmmoCounterComponent component)
    {
        if (component.Control == null)
            return;

        // <Trauma>
        if (component.Control is BoxContainer container)
        {
            foreach (var child in container.Children)
            {
                var childEv = new UpdateAmmoCounterEvent()
                {
                    Control = child
                };

                RaiseLocalEvent(uid, childEv);
            }
        }
        // </Trauma>

        var ev = new UpdateAmmoCounterEvent()
        {
            Control = component.Control
        };

        RaiseLocalEvent(uid, ev, false);
    }

    public override void UpdateAmmoCount(EntityUid uid, bool prediction = true) // Trauma - made public
    {
        // Don't use resolves because the method is shared and there's no compref and I'm trying to
        // share as much code as possible
        if (prediction && !Timing.IsFirstTimePredicted ||
            !TryComp<AmmoCounterComponent>(uid, out var clientComp))
        {
            return;
        }

        UpdateAmmoCount(uid, clientComp);
    }

    /// <summary>
    /// Raised when an ammocounter is requesting a control.
    /// </summary>
    public sealed class AmmoCounterControlEvent : EntityEventArgs
    {
        public Control? Control
        // <Trauma>
        {
            get => Controls.FirstOrDefault();
            set
            {
                if (value is not { } control)
                    return;

                var type = control.GetType();

                var index = Controls.FindIndex(x => x.GetType() == type);

                if (index < 0)
                    Controls.Add(control);
                else
                    Controls[index] = control;
            }
        }

        public List<Control> Controls = new();
        // </Trauma>
    }

    /// <summary>
    /// Raised whenever the ammo count / magazine for a control needs updating.
    /// </summary>
    public sealed class UpdateAmmoCounterEvent : HandledEntityEventArgs
    {
        // <Trauma>
        // Only used for BatteryAmmoProvider currently
        public float FireCostMultiplier = 1f;
        // </Trauma>
        public Control Control = default!;
    }

    #region Controls

    public sealed class DefaultStatusControl : Control // Trauma - made public
    {
        private readonly Label _ammoCount; // Trauma
        private readonly BulletRender _bulletRender;

        public DefaultStatusControl()
        {
            MinHeight = 15;
            HorizontalExpand = true;
            VerticalAlignment = VAlignment.Center;
            // <Trauma> - wrap BulletRender in BoxContainer, add _ammoCount
            AddChild(new BoxContainer
            {
                HorizontalExpand = true,
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                Children =
                {
                    (_bulletRender = new BulletRender
                    {
                        Margin = new Thickness(0, 0, 5, 0),
                        HorizontalExpand = true
                    }),
                    (_ammoCount = new Label
                    {
                        StyleClasses = { StyleClass.ItemStatus },
                        HorizontalAlignment = HAlignment.Right,
                        VerticalAlignment = VAlignment.Bottom
                    }),
                }
            });
            // <Trauma>
        }

        public void Update(int count, int capacity)
        {
            // <Trauma>
            _ammoCount.Visible = true;
            _ammoCount.Text = $"x{count:00}";
            // </Trauma>

            _bulletRender.Count = count;
            _bulletRender.Capacity = capacity;

            _bulletRender.Type = capacity switch
            {
                > 50 => BulletRender.BulletType.Tiny,
                > 15 => BulletRender.BulletType.Normal,
                _ => BulletRender.BulletType.Large
            };
        }
    }

    public sealed class BoxesStatusControl : Control
    {
        private readonly BatteryBulletRenderer _bullets;
        private readonly Label _ammoCount;

        public BoxesStatusControl()
        {
            MinHeight = 15;
            HorizontalExpand = true;
            VerticalAlignment = Control.VAlignment.Center;

            AddChild(new BoxContainer
            {
                HorizontalExpand = true, // Trauma
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                Children =
                {
                    (_bullets = new BatteryBulletRenderer
                    {
                        Margin = new Thickness(0, 0, 5, 0),
                        HorizontalExpand = true
                    }),
                    (_ammoCount = new Label
                    {
                        StyleClasses = { StyleClass.ItemStatus },
                        HorizontalAlignment = HAlignment.Right,
                        VerticalAlignment = VAlignment.Bottom
                    }),
                }
            });
        }

        public void Update(int count, int max)
        {
            _ammoCount.Visible = true;

            _ammoCount.Text = $"x{count:00}";

            _bullets.Capacity = max;
            _bullets.Count = count;
        }
    }

    private sealed class ChamberMagazineStatusControl : Control
    {
        private readonly BulletRender _bulletRender;
        private readonly TextureRect _chamberedBullet;
        private readonly Label _noMagazineLabel;
        private readonly Label _ammoCount;

        public ChamberMagazineStatusControl()
        {
            MinHeight = 15;
            HorizontalExpand = true;
            VerticalAlignment = Control.VAlignment.Center;

            AddChild(new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                Children =
                {
                    new Control
                    {
                        HorizontalExpand = true,
                        Margin = new Thickness(0, 0, 5, 0),
                        Children =
                        {
                            (_bulletRender = new BulletRender
                            {
                                // <Trauma>
                                HorizontalExpand = true,
                                /*
                                HorizontalAlignment = HAlignment.Right,
                                </Trauma> */
                                VerticalAlignment = VAlignment.Bottom
                            }),
                            (_noMagazineLabel = new Label
                            {
                                Text = "No Magazine!",
                                StyleClasses = {StyleClass.ItemStatus}
                            })
                        }
                    },
                    new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Vertical,
                        VerticalAlignment = VAlignment.Bottom,
                        Margin = new Thickness(0, 0, 0, 2),
                        Children =
                        {
                            (_ammoCount = new Label
                            {
                                StyleClasses = {StyleClass.ItemStatus},
                                HorizontalAlignment = HAlignment.Right,
                            }),
                            (_chamberedBullet = new TextureRect
                            {
                                Texture = StaticIoC.ResC.GetTexture("/Textures/Interface/ItemStatus/Bullets/chambered.png"),
                                HorizontalAlignment = HAlignment.Left,
                            }),
                        }
                    }
                }
            });
        }

        public void Update(bool chambered, bool magazine, int count, int capacity)
        {
            _chamberedBullet.ModulateSelfOverride =
                chambered ? Color.FromHex("#d7df60") : Color.Black;

            if (!magazine)
            {
                _bulletRender.Visible = false;
                _noMagazineLabel.Visible = true;
                _ammoCount.Visible = false;
                return;
            }

            _bulletRender.Visible = true;
            _noMagazineLabel.Visible = false;
            _ammoCount.Visible = true;

            _bulletRender.Count = count;
            _bulletRender.Capacity = capacity;

            _bulletRender.Type = capacity switch
            {
                > 50 => BulletRender.BulletType.Tiny,
                > 15 => BulletRender.BulletType.Normal,
                _ => BulletRender.BulletType.Large
            };

            _ammoCount.Text = $"x{count:00}";
        }

        public void PlayAlarmAnimation(Animation animation)
        {
            _noMagazineLabel.PlayAnimation(animation, "alarm");
        }
    }

    private sealed class RevolverStatusControl : Control
    {
        private readonly BoxContainer _bulletsList;

        public RevolverStatusControl()
        {
            MinHeight = 15;
            HorizontalExpand = true;
            VerticalAlignment = Control.VAlignment.Center;
            AddChild((_bulletsList = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Center,
                SeparationOverride = 0
            }));
        }

        public void Update(int currentIndex, bool?[] bullets)
        {
            _bulletsList.RemoveAllChildren();
            var capacity = bullets.Length;

            string texturePath;
            if (capacity <= 20)
            {
                texturePath = "/Textures/Interface/ItemStatus/Bullets/normal.png";
            }
            else if (capacity <= 30)
            {
                texturePath = "/Textures/Interface/ItemStatus/Bullets/small.png";
            }
            else
            {
                texturePath = "/Textures/Interface/ItemStatus/Bullets/tiny.png";
            }

            var texture = StaticIoC.ResC.GetTexture(texturePath);
            var spentTexture = StaticIoC.ResC.GetTexture("/Textures/Interface/ItemStatus/Bullets/empty.png");

            FillBulletRow(currentIndex, bullets, _bulletsList, texture, spentTexture);
        }

        private void FillBulletRow(int currentIndex, bool?[] bullets, Control container, Texture texture, Texture emptyTexture)
        {
            var capacity = bullets.Length;
            var colorA = Color.FromHex("#b68f0e");
            var colorB = Color.FromHex("#d7df60");
            var colorSpentA = Color.FromHex("#b50e25");
            var colorSpentB = Color.FromHex("#d3745f");
            var colorGoneA = Color.FromHex("#000000");
            var colorGoneB = Color.FromHex("#222222");

            var altColor = false;
            var scale = 1.3f;

            for (var i = 0; i < capacity; i++)
            {
                var bulletFree = bullets[i];
                // Add a outline
                var box = new Control()
                {
                    MinSize = texture.Size * scale,
                };
                if (i == currentIndex)
                {
                    box.AddChild(new TextureRect
                    {
                        Texture = texture,
                        TextureScale = new Vector2(scale, scale),
                        ModulateSelfOverride = Color.LimeGreen,
                    });
                }
                Color color;
                Texture bulletTexture = texture;

                if (bulletFree.HasValue)
                {
                    if (bulletFree.Value)
                    {
                        color = altColor ? colorA : colorB;
                    }
                    else
                    {
                        color = altColor ? colorSpentA : colorSpentB;
                        bulletTexture = emptyTexture;
                    }
                }
                else
                {
                    color = altColor ? colorGoneA : colorGoneB;
                }

                box.AddChild(new TextureRect
                {
                    Stretch = TextureRect.StretchMode.KeepCentered,
                    Texture = bulletTexture,
                    ModulateSelfOverride = color,
                });
                altColor ^= true;
                container.AddChild(box);
            }
        }
    }

    #endregion
}
