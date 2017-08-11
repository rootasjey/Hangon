using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Globalization;
using Unsplasharp.Models;
using Windows.UI.Notifications;

namespace Tasks.Services {
    public sealed class TileDesigner {
        public static void UpdatePrimary(object supposedListPhotos) {
            if (supposedListPhotos == null) return;
            var photos = (List<Photo>)supposedListPhotos;

            var tileUpdater = TileUpdateManager.CreateTileUpdaterForApplication();
            tileUpdater.Clear();
            tileUpdater.EnableNotificationQueue(true);

            for (int i = 0; i < photos.Count; i++) {
                tileUpdater.Update(CreateNotification(photos[i]));
            }
        }

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
