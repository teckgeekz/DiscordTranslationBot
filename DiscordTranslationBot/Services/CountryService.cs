﻿using DiscordTranslationBot.Models;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Services;

/// <summary>
/// Provides methods to interact with flag emojis. This should be injected as a singleton so the flag emoji list
/// doesn't have to be regenerated.
/// </summary>
public sealed class CountryService : ICountryService
{
    private readonly ILogger<CountryService> _logger;

    private readonly ISet<Country> _countries;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountryService"/> class.
    /// </summary>
    /// <param name="logger">Logger to use.</param>
    /// <exception cref="InvalidOperationException">No flag emoji found.</exception>
    public CountryService(ILogger<CountryService> logger)
    {
        _logger = logger;

        // Get all flag emojis.
        var flagEmoji = Emoji.All
            .Where(e => e.Group == "Flags" && e.Subgroup == "country-flag");

        _countries = flagEmoji
            .Select(e => new Country(
                e.ToString(),
                e.Name?.Replace("flag: ", string.Empty, StringComparison.Ordinal)))
            .ToHashSet();

        if (!_countries.Any())
        {
            _logger.LogCritical("No flag emoji found.");
            throw new InvalidOperationException("No flag emoji found.");
        }

        InitializeSupportedLangCodes();
    }

    /// <inheritdoc cref="ICountryService.TryGetCountry"/>
    public bool TryGetCountry(string emojiUnicode, out Country? country)
    {
        country = _countries.SingleOrDefault(c => c.EmojiUnicode == emojiUnicode);
        return country != null;
    }

    /// <summary>
    /// Initializes supported language codes to countries.
    /// </summary>
    private void InitializeSupportedLangCodes()
    {
        SetLangCodes(Emoji.FlagAustralia, "en");
        SetLangCodes(Emoji.FlagCanada, "en");
        SetLangCodes(Emoji.FlagUnitedKingdom, "en");
        SetLangCodes(Emoji.FlagUnitedStates, "en");
        SetLangCodes(Emoji.FlagUsOutlyingIslands, "en");
        SetLangCodes(Emoji.FlagAlgeria, "ar");
        SetLangCodes(Emoji.FlagBahrain, "ar");
        SetLangCodes(Emoji.FlagEgypt, "ar");
        SetLangCodes(Emoji.FlagSaudiArabia, "ar");
        SetLangCodes(Emoji.FlagChina, "zh-Hans", "zh");
        SetLangCodes(Emoji.FlagHongKongSarChina, "zh-Hant", "zh");
        SetLangCodes(Emoji.FlagTaiwan, "zh-Hant", "zh");
        SetLangCodes(Emoji.FlagFrance, "fr");
        SetLangCodes(Emoji.FlagGermany, "de");
        SetLangCodes(Emoji.FlagIndia, "hi");
        SetLangCodes(Emoji.FlagIreland, "ga");
        SetLangCodes(Emoji.FlagItaly, "it");
        SetLangCodes(Emoji.FlagJapan, "ja");
        SetLangCodes(Emoji.FlagSouthKorea, "ko");
        SetLangCodes(Emoji.FlagBrazil, "pt-br", "pt");
        SetLangCodes(Emoji.FlagPortugal, "pt-pt", "pt");
        SetLangCodes(Emoji.FlagRussia, "ru");
        SetLangCodes(Emoji.FlagMexico, "es");
        SetLangCodes(Emoji.FlagSpain, "es");
        SetLangCodes(Emoji.FlagVietnam, "vi");
        SetLangCodes(Emoji.FlagThailand, "th");
    }

    /// <summary>
    /// Maps language codes to a country.
    /// </summary>
    /// <param name="flagEmoji">Flag emoji.</param>
    /// <param name="langCodes">Language codes to add.</param>
    /// <exception cref="InvalidOperationException">Country couldn't be found.</exception>
    private void SetLangCodes(SingleEmoji flagEmoji, params string[] langCodes)
    {
        var country = _countries.SingleOrDefault(c => c.EmojiUnicode == flagEmoji.ToString());
        if (country == null)
        {
            _logger.LogCritical("Country language codes couldn't be initialized as country couldn't be found.");
            throw new InvalidOperationException("Country language codes couldn't be initialized as country couldn't be found.");
        }

        country.LangCodes = langCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
