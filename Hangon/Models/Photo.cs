using System.Collections.Generic;

namespace Hangon.Models {
    public class Photo {

        #region simple properties
        public string Id { get; set; }

        public string CreatedAt { get; set; }

        public string UpdatedAt { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public string Color { get; set; }

        private int _Downloads;

        public int Downloads {
            get { return _Downloads; }
            set { _Downloads = value; }
        }


        private int _Likes;

        public int Likes {
            get {
                return _Likes;
            }
            set {
                if (_Likes != value) {
                    _Likes = value;
                }
            }
        }

        private bool _IsLikedByUser;

        public bool IsLikedByUser {
            get { return _IsLikedByUser; }
            set { _IsLikedByUser = value; }
        }

        #endregion simple properties

        #region composed properties
        public List<Collection> CurrentUserCollection { get; set; }

        public Urls Urls { get; set; }

        public List<Categories> Categories { get; set; }

        public User User { get; set; }

        public Exif Exif { get; set; }

        public Location Location { get; set; }

        public PhotoLinks Links { get; set; }

        #endregion composed properties
    }

    public class PhotoLinks {
        public string Self { get; set; }

        public string Html { get; set; }

        public string Download { get; set; }
        public string DownloadLocation { get; set; }
    }

    public class Exif {
        public string Make { get; set; }

        public string Model { get; set; }

        public string ExposureTime { get; set; }

        public string Aperture { get; set; }

        public string FocalLength { get; set; }

        public int? Iso { get; set; }
    }

    public class Location {
        private string _City;

        public string City {
            get { return _City; }
            set { _City = value; }
        }

        private string _Country;

        public string Country {
            get { return _Country; }
            set { _Country = value; }
        }

        private Position _Position;

        public Position Position {
            get { return _Position; }
            set { _Position = value; }
        }

    }

    public class Position {
        private int _Latitude;

        public int Latitude {
            get { return _Latitude; }
            set { _Latitude = value; }
        }

        private int _Longitude;

        public int Longitude {
            get { return _Longitude; }
            set { _Longitude = value; }
        }

    }

    public class Urls {
        /// <summary>
        /// Photo in native resolution and uncompressed
        /// </summary>
        public string Raw { get; set; }

        public string Full { get; set; }

        public string Regular { get; set; }

        public string Small { get; set; }

        public string Thumbnail { get; set; }
    }

    public class Categories {
        public string Id { get; set; }

        public string Title { get; set; }

        public int PhotoCount { get; set; }

        public CategoriesLinks Links { get; set; }
    }

    public class CategoriesLinks {
        public string Self { get; set; }

        public string Photos { get; set; }

    }
}

