namespace Hangon.Models {
    public class User {
        #region simple properties
        public string Id { get; set; }

        public string Username { get; set; }

        public string Name { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string PortfolioUrl { get; set; }

        public string Bio { get; set; }

        public string Location { get; set; }

        public int TotalLikes { get; set; }

        public int TotalPhotos { get; set; }

        public int TotalCollections { get; set; }

        public string UpdatedAt { get; set; }

        private bool _FollowedByUser;

        public bool FollowedByUser {
            get { return _FollowedByUser; }
            set { _FollowedByUser = value; }
        }

        private int _FollowersCount;

        public int FollowersCount {
            get { return _FollowersCount; }
            set { _FollowersCount = value; }
        }

        public int FollowingCount { get; set; }

        private int _Downloads;

        public int Downloads {
            get { return _Downloads; }
            set { _Downloads = value; }
        }

        #endregion simple properties

        #region composed properties
        private ProfileImage _ProfileImage;

        public ProfileImage ProfileImage {
            get { return _ProfileImage; }
            set { _ProfileImage = value; }
        }

        private Badge _Badge;

        public Badge Badge {
            get { return _Badge; }
            set { _Badge = value; }
        }

        private UserLinks _Links;

        public UserLinks Links {
            get { return _Links; }
            set { _Links = value; }
        }
        #endregion composed properties
    }

    public class ProfileImage {
        public string Small { get; set; }

        public string Medium { get; set; }

        public string Large { get; set; }
    }

    public class Badge {
        public string Title { get; set; }

        public bool Primary { get; set; }

        public string Slug { get; set; }

        public string Link { get; set; }
    }

    public class UserLinks {
        public string Self { get; set; }

        public string Html { get; set; }

        public string Photos { get; set; }

        public string Likes { get; set; }

        public string Portfolio { get; set; }
    }
}
