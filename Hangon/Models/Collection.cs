namespace Hangon.Models {
    public class Collection {

        #region simple properties

        public string Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string PublishedAt { get; set; }

        public string UpdatedAt { get; set; }

        public bool IsCurrated { get; set; }

        public bool IsFeatured { get; set; }

        public int TotalPhotos { get; set; }

        public bool IsPrivate { get; set; }

        public string ShareKey { get; set; }

        #endregion simple properties

        #region composed properties

        public Photo CoverPhoto { get; set; }

        public User User { get; set; }

        public CollectionLinks Links { get; set; }

        #endregion composed properties
    }

    public class CollectionLinks {
        public string Self { get; set; }
        public string Html { get; set; }
        public string Photos { get; set; }
        public string Related { get; set; }
    }
}
