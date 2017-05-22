using System.Collections.Generic;

namespace Hangon.Models {
    public class Photo {

        #region simple properties
        private string _Id;
        public string Id {
            get {
                return _Id;
            }
            set {
                if (_Id != value) {
                    _Id = value;
                }
            }
        }

        private string _CreatedAt;

        public string CreatedAt {
            get { return _CreatedAt; }
            set { _CreatedAt = value; }
        }

        private string _UpdatedAt;

        public string UpdatedAt {
            get { return _UpdatedAt; }
            set { _UpdatedAt = value; }
        }

        private int _Width;

        public int Width {
            get { return _Width; }
            set { _Width = value; }
        }

        private int _Height;

        public int Height {
            get { return _Height; }
            set { _Height = value; }
        }

        private string _Color;

        public string Color {
            get { return _Color; }
            set { _Color = value; }
        }

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

        private bool _LikedByUser;

        public bool LikedByUser {
            get { return _LikedByUser; }
            set { _LikedByUser = value; }
        }

        #endregion simple properties

        #region composed properties
        private List<Collection> _CurrentUserCollection;

        public List<Collection> CurrentUserCollection {
            get { return _CurrentUserCollection; }
            set { _CurrentUserCollection = value; }
        }

        private Urls _Urls;

        public Urls Urls {
            get { return _Urls; }
            set { _Urls = value; }
        }

        private List<Categories> _Categories;

        public List<Categories> Categories {
            get { return _Categories; }
            set { _Categories = value; }
        }

        private User _User;

        public User User {
            get { return _User; }
            set { _User = value; }
        }

        #endregion composed properties
    }

    public class PhotoLinks {
        public string Self { get; set; }

        public string Html { get; set; }

        public string Download { get; set; }
        public string DownloadLocation { get; set; }
    }

    public class Exif {
        private string _Make;

        public string Make {
            get { return _Make; }
            set { _Make = value; }
        }

        private string _Model;

        public string Model {
            get { return _Model; }
            set { _Model = value; }
        }

        private string _ExposureTime;

        public string ExposureTime {
            get { return _ExposureTime; }
            set { _ExposureTime = value; }
        }

        private string _Aperture;

        public string Aperture {
            get { return _Aperture; }
            set { _Aperture = value; }
        }

        private string _FocalLength;

        public string FocalLength {
            get { return _FocalLength; }
            set { _FocalLength = value; }
        }

        private int _Iso;

        public int Iso {
            get { return _Iso; }
            set { _Iso = value; }
        }
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

