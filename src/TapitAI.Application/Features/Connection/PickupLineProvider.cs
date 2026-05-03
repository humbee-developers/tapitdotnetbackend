namespace TapitAI.Application.Features.Connection;

internal static class PickupLineProvider
{
    private static readonly string[] Lines =
    [
        "Hey there! Someone nearby thinks you look amazing 😊",
        "A nearby match is hoping to connect with you!",
        "You've caught someone's eye nearby — they'd love to meet you!",
        "Something tells me you two would get along great ✨",
        "Life is short — why not make a new connection nearby? 😄",
        "A friendly neighbor would love to say hello!",
        "There's someone close by who'd love to get to know you!",
        "Sparks might fly — someone nearby wants to connect!",
        "Your next great story might start right here 🌟",
        "Hey! A nearby match thinks you're worth knowing 💫",
        "Someone close by is hoping you'll say yes!",
        "A new adventure might be just around the corner 🗺️",
        "You never know — this could be the start of something wonderful!",
        "Fate brought you two close together — make the most of it!",
        "A local connection is waiting to happen ✌️"
    ];

    internal static string GetRandom() => Lines[Random.Shared.Next(Lines.Length)];
}
