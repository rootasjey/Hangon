﻿using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Globalization;
using Unsplasharp.Models;
using Windows.UI.Notifications;

namespace Hangon.Services {
    public class TileDesigner {
        public static void UpdatePrimary() {
            var data = App.AppDataSource;
            if (data == null) return;

            var rand = new Random();
            var maxCount = data.RecentPhotos.Count;
            var start = rand.Next(maxCount);
            start = start + 5 > maxCount ? (start - 5) : start;

            var tileUpdater = TileUpdateManager.CreateTileUpdaterForApplication();
            tileUpdater.Clear();
            tileUpdater.EnableNotificationQueue(true);

            // 1st notification is transparent to see background image
            // through it at the begining
            tileUpdater.Update(CreateTransparentNotifications());

            for (int i = start; i < (start + 4); i++) {
                tileUpdater.Update(CreateNotification(data.RecentPhotos[i]));
            }
        }

        public static void ClearPrimary() {
            var tileUpdater = TileUpdateManager.CreateTileUpdaterForApplication();
            tileUpdater.Clear();
        }

        #region transparent notification

        private static TileNotification CreateTransparentNotifications() {
            var content = new TileContent() {
                Visual = new TileVisual() {
                    TileMedium = CreateTransparentBinding(),
                    TileWide = CreateTransparentBinding(),
                    TileLarge = CreateTransparentBinding()
                }
            };

            return new TileNotification(content.GetXml());
        }

        private static TileBinding CreateTransparentBinding() {
            return new TileBinding() {
                Content = new TileBindingContentAdaptive() {
                    TextStacking = TileTextStacking.Center,
                    PeekImage = new TilePeekImage() {
                        Source = "Assets/Square150x150Logo.scale-200.png"
                    },
                    Children = {
                        new AdaptiveText() {
                            Text = "Hangon",
                            HintAlign = AdaptiveTextAlign.Center,
                            HintStyle = AdaptiveTextStyle.Subheader
                        }
                    }
                }
            };
        }

        #endregion transparent notification

        private static TileNotification CreateNotification(Photo photo) {
            var content = new TileContent() {
                Visual = new TileVisual() {
                    TileMedium = CreateBinding(photo),
                    TileWide = CreateBinding(photo),
                    TileLarge = CreateBinding(photo)
                }
            };

            return new TileNotification(content.GetXml());
        }

        private static TileBinding CreateBinding(Photo photo) {
            var createdAt = ParseUniversalDateTime(photo.CreatedAt);
            var username = photo.User.Username ??
                photo.User.Name ??
                string.Format("{0} {1}", photo.User.FirstName, photo.User.LastName);

            return new TileBinding() {
                Content = new TileBindingContentAdaptive() {
                    TextStacking = TileTextStacking.Center,
                    PeekImage = new TilePeekImage() {
                        Source = photo.Urls.Regular
                    },
                    Children = {
                        new AdaptiveText() {
                            Text = username,
                            HintAlign = AdaptiveTextAlign.Center,
                            HintStyle = AdaptiveTextStyle.Base
                        },
                        new AdaptiveText() {
                            Text = createdAt.ToString("dd MMM yyyy"),
                            HintAlign = AdaptiveTextAlign.Center,
                            HintStyle = AdaptiveTextStyle.CaptionSubtle
                        },
                        new AdaptiveText() {
                            Text = photo.Exif?.Make,
                            HintAlign = AdaptiveTextAlign.Center,
                            HintStyle = AdaptiveTextStyle.Caption
                        },
                        new AdaptiveText() {
                            Text = photo.Exif?.Model,
                            HintAlign = AdaptiveTextAlign.Center,
                            HintStyle = AdaptiveTextStyle.CaptionSubtle
                        }
                    }
                }
            };
        }

        private static DateTime ParseUniversalDateTime(string str) {
            return DateTime.ParseExact(str, DateTimeFormats,
                CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        static readonly string[] DateTimeFormats = {
            "MM/dd/yyyy HH:mm:ss"
        };
    }
}
