using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.IO;
using Telegram.Bot.Types.InputFiles;
namespace TgMusicBot
{
    class Program
    {
        private static string botToken = "1876036858:AAG22I-yWk0LagB_AhL1XFYzdtwtSeizUIA";
        private static TelegramBotClient botClient;

        static async Task Main(string[] args)
        {
            botClient = new TelegramBotClient(botToken);

            var me = await botClient.GetMeAsync();
            Console.WriteLine($"Запущен бот {me.Username}");

            using var cts = new CancellationTokenSource();
            botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                new ReceiverOptions { AllowedUpdates = { } },
                cancellationToken: cts.Token
            );

            Console.WriteLine("Нажмите любую клавишу для остановки бота...");
            Console.ReadKey();
            cts.Cancel();
        }

        // Обработка сообщений от пользователей
        static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
            {
                var chatId = update.Message.Chat.Id;
                var messageText = update.Message.Text;

                Console.WriteLine($"Получено сообщение: '{messageText}' в чате {chatId}");

                if (messageText.StartsWith("/music "))
                {
                    string query = messageText.Substring(7);
                    string filePath = await DownloadMusicFromYouTubeAsync(query);

                    if (!string.IsNullOrEmpty(filePath))
                    {
                        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            await botClient.SendAudioAsync(chatId, new InputOnlineFile(fileStream), cancellationToken: cancellationToken);
                        }

                        Console.WriteLine("Музыка отправлена пользователю.");
                        System.IO.File.Delete(filePath); // Удаляем файл после отправки
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "Не удалось найти музыку. Попробуйте другой запрос.", cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Отправьте команду /music <запрос> для поиска музыки.", cancellationToken: cancellationToken);
                }
            }
        }

        // Поиск и загрузка музыки с YouTube
        static async Task<string> DownloadMusicFromYouTubeAsync(string query)
        {
            string fileName = "downloaded_music.mp3";
            string arguments = $"ytsearch1:\"{query}\" -x --audio-format mp3 -o \"{fileName}\"";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0 && System.IO.File.Exists(fileName))
                    {
                        return fileName;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке: {ex.Message}");
            }

            return null;
        }

        // Обработка ошибок
        static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Ошибка API Telegram: {apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }
    }
}
