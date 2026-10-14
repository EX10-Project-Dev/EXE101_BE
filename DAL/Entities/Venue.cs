namespace DAL.Entities;

public enum VenueStatus { Pending, Published, Closed, Rejected }
public enum PriceBasis { PerPerson, PerItem }

public class Venue : BaseEntity
{
    public string? SourceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    public PriceBasis? PriceBasis { get; set; }
    public string? PriceNote { get; set; }
    public string? SourceDataJson { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public string? GooglePlaceId { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? MenuUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public VenueStatus Status { get; set; } = VenueStatus.Pending;
    public string? CreatedByUserId { get; set; }

    public VenueCategory Category { get; set; } = null!;
    public ICollection<VenuePhoto> Photos { get; set; } = new List<VenuePhoto>();
    public ICollection<VenueOpeningHour> OpeningHours { get; set; } = new List<VenueOpeningHour>();
    public ICollection<VenueHoursException> HoursExceptions { get; set; } = new List<VenueHoursException>();
    public ICollection<VenueTagLink> Tags { get; set; } = new List<VenueTagLink>();
    public ICollection<VenueAmenityLink> Amenities { get; set; } = new List<VenueAmenityLink>();
}

public class VenueCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<Venue> Venues { get; set; } = new List<Venue>();
}

public enum VenueTagType { Purpose, Atmosphere }
public class VenueTag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public VenueTagType TagType { get; set; }
    public ICollection<VenueTagLink> Venues { get; set; } = new List<VenueTagLink>();
}

public class VenueTagLink
{
    public Guid VenueId { get; set; }
    public Guid VenueTagId { get; set; }
    public Venue Venue { get; set; } = null!;
    public VenueTag Tag { get; set; } = null!;
}

public enum AmenityAvailability { Available, Unavailable, Unknown }
public class Amenity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<VenueAmenityLink> Venues { get; set; } = new List<VenueAmenityLink>();
}

public class VenueAmenityLink
{
    public Guid VenueId { get; set; }
    public Guid AmenityId { get; set; }
    public AmenityAvailability Availability { get; set; } = AmenityAvailability.Unknown;
    public string? Note { get; set; }
    public Venue Venue { get; set; } = null!;
    public Amenity Amenity { get; set; } = null!;
}

public class VenueOpeningHour : BaseEntity
{
    public Guid VenueId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
    public bool IsClosed { get; set; }
    public string? Note { get; set; }
    public Venue Venue { get; set; } = null!;
}

public class VenueHoursException : BaseEntity
{
    public Guid VenueId { get; set; }
    public DateOnly Date { get; set; }
    public bool IsClosed { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
    public string? Note { get; set; }
    public Venue Venue { get; set; } = null!;
}

public enum VenuePhotoType { Exterior, Interior, Menu, Other }
public class VenuePhoto : BaseEntity
{
    public Guid VenueId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public VenuePhotoType PhotoType { get; set; }
    public string? Caption { get; set; }
    public int SortOrder { get; set; }
    public string? Source { get; set; }
    public string? UsagePermission { get; set; }
    public Venue Venue { get; set; } = null!;
}
