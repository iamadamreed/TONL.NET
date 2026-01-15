using System.Text.Json.Serialization;

namespace TONL.Benchmarks.Models;

/// <summary>
/// User model matching sample-users.json fixture.
/// </summary>
public record User(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("active")] bool Active,
    [property: JsonPropertyName("tags")] string[] Tags,
    [property: JsonPropertyName("lastLogin")] DateTime LastLogin
);

/// <summary>
/// Product container matching ecommerce-products.json fixture.
/// </summary>
public record ProductsContainer(
    [property: JsonPropertyName("products")] Product[] Products
);

/// <summary>
/// Product model matching ecommerce-products.json fixture.
/// </summary>
public record Product(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("brand")] string Brand,
    [property: JsonPropertyName("specs")] Dictionary<string, object?> Specs,
    [property: JsonPropertyName("inventory")] int Inventory,
    [property: JsonPropertyName("reviews")] Review[] Reviews
);

/// <summary>
/// Review model for product reviews.
/// </summary>
public record Review(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("rating")] int Rating,
    [property: JsonPropertyName("comment")] string Comment,
    [property: JsonPropertyName("reviewer")] string Reviewer,
    [property: JsonPropertyName("date")] string Date
);

/// <summary>
/// API response model matching api-response.json fixture.
/// </summary>
public record ApiResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] ApiData Data,
    [property: JsonPropertyName("meta")] ApiMeta Meta
);

public record ApiData(
    [property: JsonPropertyName("users")] ApiUser[] Users,
    [property: JsonPropertyName("pagination")] Pagination Pagination
);

public record ApiUser(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("profile")] Profile Profile,
    [property: JsonPropertyName("preferences")] Preferences Preferences,
    [property: JsonPropertyName("activity")] Activity Activity
);

public record Profile(
    [property: JsonPropertyName("first_name")] string FirstName,
    [property: JsonPropertyName("last_name")] string LastName,
    [property: JsonPropertyName("avatar")] string Avatar,
    [property: JsonPropertyName("bio")] string Bio,
    [property: JsonPropertyName("location")] Location Location
);

public record Location(
    [property: JsonPropertyName("city")] string City,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("country")] string Country,
    [property: JsonPropertyName("coordinates")] Coordinates Coordinates
);

public record Coordinates(
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude
);

public record Preferences(
    [property: JsonPropertyName("theme")] string Theme,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("notifications")] Notifications Notifications
);

public record Notifications(
    [property: JsonPropertyName("email")] bool Email,
    [property: JsonPropertyName("push")] bool Push,
    [property: JsonPropertyName("sms")] bool Sms
);

public record Activity(
    [property: JsonPropertyName("last_login")] DateTime LastLogin,
    [property: JsonPropertyName("login_count")] int LoginCount,
    [property: JsonPropertyName("sessions")] Session[] Sessions
);

public record Session(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("start_time")] DateTime StartTime,
    [property: JsonPropertyName("duration")] int Duration,
    [property: JsonPropertyName("ip_address")] string IpAddress,
    [property: JsonPropertyName("user_agent")] string UserAgent
);

public record Pagination(
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("per_page")] int PerPage,
    [property: JsonPropertyName("total_pages")] int TotalPages,
    [property: JsonPropertyName("total_items")] int TotalItems,
    [property: JsonPropertyName("has_next")] bool HasNext,
    [property: JsonPropertyName("has_prev")] bool HasPrev
);

public record ApiMeta(
    [property: JsonPropertyName("timestamp")] DateTime Timestamp,
    [property: JsonPropertyName("request_id")] string RequestId,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("server_info")] ServerInfo ServerInfo
);

public record ServerInfo(
    [property: JsonPropertyName("region")] string Region,
    [property: JsonPropertyName("instance_id")] string InstanceId
);
