using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using DAL.Entities;

namespace BLL.Services;

public static class VenueMediaHelper
{
    public static readonly Dictionary<string, string> CategoryDefaultImages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CAFE"] = "https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb?auto=format&fit=crop&w=1200&q=80",
        ["RESTAURANT"] = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&w=1200&q=80",
        ["EATERY"] = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=1200&q=80",
        ["TEA_DRINKS"] = "https://images.unsplash.com/photo-1558857563-b37cf5429e57?auto=format&fit=crop&w=1200&q=80",
        ["TEA_COFFEE"] = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?auto=format&fit=crop&w=1200&q=80",
        ["BAKERY_DESSERT"] = "https://images.unsplash.com/photo-1509440159596-0249088772ff?auto=format&fit=crop&w=1200&q=80",
        ["VIETNAMESE_RESTAURANT"] = "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=1200&q=80",
        ["ICE_CREAM"] = "https://images.unsplash.com/photo-1497034825429-c343d7c6a68f?auto=format&fit=crop&w=1200&q=80",
        ["MALA_HOTPOT_FAST_FOOD"] = "https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=1200&q=80",
        ["OTHER"] = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&w=1200&q=80",
        ["DEFAULT"] = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&w=1200&q=80"
    };

    public static string GetDefaultImageUrl(string? categoryCode)
    {
        if (!string.IsNullOrWhiteSpace(categoryCode) && CategoryDefaultImages.TryGetValue(categoryCode, out var img))
            return img;
        return CategoryDefaultImages["DEFAULT"];
    }

    public static bool IsValidImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        var clean = CleanImageUrl(url);
        if (clean.Contains("google.com/maps", StringComparison.OrdinalIgnoreCase) ||
            clean.Contains("maps/search", StringComparison.OrdinalIgnoreCase) ||
            clean.Contains("google. com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return Uri.TryCreate(clean, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    public static string CleanImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        var trimmed = url.Trim();
        var match = Regex.Match(trimmed, @"https?://[^\s]+");
        return match.Success ? match.Value : trimmed.Replace(" ", "");
    }

    public static string BuildGoogleMapsUrl(Venue venue, IReadOnlyDictionary<string, string?>? sourceData = null)
    {
        if (!string.IsNullOrWhiteSpace(venue.GooglePlaceId))
        {
            var cleanPlaceId = Regex.Match(venue.GooglePlaceId, @"ChIJ[A-Za-z0-9_-]+").Value;
            if (!string.IsNullOrWhiteSpace(cleanPlaceId))
            {
                return $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(venue.Name)}&query_place_id={cleanPlaceId}";
            }
        }

        if (sourceData != null && sourceData.TryGetValue("googleMapsUrlSource", out var rawUrl) && !string.IsNullOrWhiteSpace(rawUrl))
        {
            var cleaned = CleanGoogleUrl(rawUrl, venue);
            if (!string.IsNullOrWhiteSpace(cleaned))
                return cleaned;
        }

        var lat = venue.Latitude.ToString(CultureInfo.InvariantCulture);
        var lon = venue.Longitude.ToString(CultureInfo.InvariantCulture);
        return $"https://www.google.com/maps/search/?api=1&query={lat},{lon}";
    }

    public static string CleanGoogleUrl(string raw, Venue? venue = null)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var cleaned = raw.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("&amp;", "&");

        var placeIdMatch = Regex.Match(cleaned, @"ChIJ[A-Za-z0-9_-]+");
        if (placeIdMatch.Success)
        {
            var placeId = placeIdMatch.Value;
            var venueName = venue != null ? Uri.EscapeDataString(venue.Name) : "place";
            return $"https://www.google.com/maps/search/?api=1&query={venueName}&query_place_id={placeId}";
        }

        var coordMatch = Regex.Match(cleaned, @"query=(\d+\.?\d*,\d+\.?\d*)");
        if (coordMatch.Success)
        {
            return $"https://www.google.com/maps/search/?api=1&query={coordMatch.Groups[1].Value}";
        }

        if (venue != null && venue.Latitude != 0 && venue.Longitude != 0)
        {
            var lat = venue.Latitude.ToString(CultureInfo.InvariantCulture);
            var lon = venue.Longitude.ToString(CultureInfo.InvariantCulture);
            return $"https://www.google.com/maps/search/?api=1&query={lat},{lon}";
        }

        return cleaned;
    }
}
