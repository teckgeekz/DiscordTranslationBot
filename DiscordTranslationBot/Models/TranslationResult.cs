﻿namespace DiscordTranslationBot.Models;

public sealed class TranslationResult
{
    public string ProviderName { get; set; }

    public string? DetectedLanguageCode { get; set; }

    public string TargetLanguageCode { get; set; }

    public string TranslatedText { get; set; }
}
