using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Chat;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Microsoft.UI;
//using ChatMessage = OpenAI.ObjectModels.RequestModels.ChatMessage;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Reflection;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using ChatMessage = OpenAI.ObjectModels.RequestModels.ChatMessage;
using static OpenAI.ObjectModels.StaticValues;



// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SugrFree.Pages
{

    public sealed partial class SugrFreePage : Page
    {
        private OpenAIService openAiService;
        private StorageFile file = null;
        public string urlForAlbumArtGen;
        private List<ChatMessage> conversationContext = new List<ChatMessage>();
        public SugrFreePage()
        {
            //You are SugrFree AI: A Generative AI who is an expert nutritionist. Always ask patients' dietary preferences (veg/non-veg/vegan, etc.) before answering. You will confidently answer questions about nutritional information and if it's recommended for diabetic people to eat it. Keep your responses short. Do no hallucinate information that was not provided to you although attempt answering questions only if you confidently know the context. Limit your meal plans to the Indian cuisine preferably South Indian cuisine. 
            this.InitializeComponent();
            var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = openAiKey
            });
            conversationContext.Add(ChatMessage.FromUser("You are SugrFree AI: A Generative AI who is an expert nutritionist. Always ask patients' dietary preferences (veg/non-veg/vegan, etc.) before answering. You will confidently answer questions about nutritional information and if it's recommended for diabetic people to eat it. Keep your responses short. Do no hallucinate information that was not provided to you although attempt answering questions only if you confidently know the context. Limit your meal plans to the Indian cuisine preferably South Indian cuisine."));
            GeneratingProgressBar.Visibility = Visibility.Collapsed;
            UploadedBorder.Visibility = Visibility.Collapsed;
            UploadedImage.Visibility = Visibility.Collapsed;
            UploadedTextBlock.Visibility = Visibility.Collapsed;    
        }

        private async void PickAFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear previous returned file name, if it exists, between iterations of this scenario
            PickAFileOutputTextBlock.Text = "";

            // Create a file picker
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

            // See the sample code below for how to make the window accessible from the App class.
            var window = new MainWindow();

            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            // Initialize the file picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            // Set options for your file picker
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.FileTypeFilter.Add("*");

            // Open the picker for the user to pick a file
            file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                PickAFileOutputTextBlock.Text = "Picked file: " + file.Name;
            }
            else
            {
                PickAFileOutputTextBlock.Text = "Image upload cancelled.";
            }
        }

        // ...

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string userInput = InputTextBox.Text;
            if (!string.IsNullOrEmpty(userInput))
            {
                GeneratingProgressBar.Visibility = Visibility.Visible;
                if (file != null) // Image file case
                {
                    AddMessageToConversation($"You: {userInput}");
                    InputTextBox.Text = string.Empty;

                    // Convert StorageFile to BitmapImage
                    var bitmapImage = new BitmapImage();
                    using (var stream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        await bitmapImage.SetSourceAsync(stream);
                    }
                    UploadedBorder.Visibility = Visibility.Visible;
                    UploadedImage.Visibility = Visibility.Visible;
                    UploadedTextBlock.Visibility = Visibility.Visible;
                    UploadedImage.Source = bitmapImage;

                    var binaryImage = File.ReadAllBytesAsync(file.Path);
                    // Add the file as an image Path to the conversation context and analyze it using GPT-4o vision model
                    var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
                    {
                        Messages = new List<ChatMessage>
                        {
                            ChatMessage.FromSystem("You are an expert Nutritionist. Look at the image and identify the dish. Always search the internet and provide nutrition values (as numbers) for a standard serving in grams or ml. Strictly Do not use Markdown format, use plaintext. Keep responses short."),
                            ChatMessage.FromUser(
                                new List<MessageContent>
                                {
                                    MessageContent.TextContent(userInput), // User's text input
                                    MessageContent.ImageBinaryContent(await binaryImage, ImageStatics.ImageFileTypes.Png, ImageStatics.ImageDetailTypes.High) // Analyze the image from file URL
                                }
                            )
                        },
                        MaxTokens = 300, // Limit tokens for image description
                        Model = Models.Gpt_4o,
                        N = 1
                    });

                    if (completionResult != null && completionResult.Successful)
                    {
                        GeneratingProgressBar.Visibility = Visibility.Collapsed;
                        AddMessageToConversation("SugrFree AI: " + completionResult.Choices.First().Message.Content);
                        conversationContext.Add(completionResult.Choices.First().Message); // Add AI response to conversation context
                    }
                    else
                    {
                        GeneratingProgressBar.Visibility = Visibility.Collapsed;
                        AddMessageToConversation("SugrFree AI: Sorry, something went wrong. " + completionResult.Error?.Message);
                    }
                }
                else // Text-only case
                {
                    AddMessageToConversation($"You: {userInput}");
                    InputTextBox.Text = string.Empty;
                    UploadedBorder.Visibility = Visibility.Collapsed;
                    UploadedImage.Visibility = Visibility.Collapsed;
                    UploadedTextBlock.Visibility = Visibility.Collapsed;

                    conversationContext.Add(ChatMessage.FromUser(userInput)); // Add user input to conversation context
                    GeneratingProgressBar.Visibility = Visibility.Visible;

                    var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
                    {
                        Messages = conversationContext, // Use conversation context
                        Model = Models.Gpt_4o,
                        MaxTokens = 800
                    });

                    if (completionResult != null && completionResult.Successful)
                    {
                        GeneratingProgressBar.Visibility = Visibility.Collapsed;
                        AddMessageToConversation("SugrFree AI: " + completionResult.Choices.First().Message.Content);
                        conversationContext.Add(completionResult.Choices.First().Message); // Add AI response to conversation context
                    }
                    else
                    {
                        GeneratingProgressBar.Visibility = Visibility.Collapsed;
                        AddMessageToConversation("SugrFree AI: Sorry, something went wrong. " + completionResult.Error?.Message);
                    }
                }
            }
        }

        private void InputTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && !string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                SendButton_Click(this, new RoutedEventArgs());
            }
        }
        public class MessageItem
        {
            public string Text { get; set; }
            public SolidColorBrush Color { get; set; }
        }
        private void AddMessageToConversation(string message)
        {
            var messageItem = new MessageItem();
            messageItem.Text = message;
            messageItem.Color = message.StartsWith("You:") ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.White);
            ConversationList.Items.Add(messageItem);

            // handle scrolling
            ConversationScrollViewer.UpdateLayout();
            ConversationScrollViewer.ChangeView(null, ConversationScrollViewer.ScrollableHeight, null);

        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            
            try
            {
                file = null;
                // Delete the file synchronously
                
                Console.WriteLine("File deleted successfully.");
            }
            catch (AggregateException ex)
            {
                foreach (var innerException in ex.InnerExceptions)
                {
                    Console.WriteLine($"Error deleting file: {innerException.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
            }

            // Clear conversation window
            ConversationList.Items.Clear();
            PickAFileOutputTextBlock.Text = "";
            UploadedBorder.Visibility = Visibility.Collapsed;
            UploadedImage.Visibility = Visibility.Collapsed;
            UploadedTextBlock.Visibility = Visibility.Collapsed;
            UploadedImage.Source = null;
            conversationContext.Clear();
            conversationContext.Add(ChatMessage.FromUser("You are SugrFree AI: A Generative AI who is an expert nutritionist. Always ask patients' dietary preferences (veg/non-veg/vegan, etc.) before answering. You will confidently answer questions about nutritional information and if it's recommended for diabetic people to eat it. Keep your responses short. Do no hallucinate information that was not provided to you although attempt answering questions only if you confidently know the context. Limit your meal plans to the Indian cuisine preferably South Indian cuisine."));

        }



    }
}
