// The predictor exercises Reflect's lookup cache without loading MelonLoader/Unity.
// Reflect.Miss reports a failed lookup through MelonLogger.Error (1.3.5+), so the stub
// has to expose the same surface the real MelonLoader does.
namespace MelonLoader
{
    internal static class MelonLogger
    {
        public static void Warning(string message) { System.Console.WriteLine(message); }
        public static void Error(string message) { System.Console.WriteLine(message); }
    }
}
