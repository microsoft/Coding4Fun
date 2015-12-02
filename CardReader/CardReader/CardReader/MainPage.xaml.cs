using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace CardReader
{
    public sealed partial class MainPage : Page
    {
        private DeviceInformationCollection _allVideoDevices;
        private DeviceInformation _desiredDevice;
        private MediaCapture _mediaCapture;
        private StorageFile _photoFile;
        private OcrEngine _ocrEngine;
        private string _phoneNumber = string.Empty;

        public MainPage()
        {
            InitializeComponent();
            Loaded += MainPage_LoadedAsync;

            // Init OCR engine with English language.
            _ocrEngine = OcrEngine.TryCreateFromLanguage(new Language("en"));
        }

        private async void MainPage_LoadedAsync(object sender, RoutedEventArgs e)
        {
            // Get available devices for capturing media and list them 
            _allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            if (_allVideoDevices == null || !_allVideoDevices.Any())
            {
                Debug.WriteLine("No devices found.");
                return;
            }
            //add to  device list
            foreach (DeviceInformation camera in _allVideoDevices)
            {
                if (CameraSelectionList.Items != null)
                {
                    CameraSelectionList.Items.Add(camera.Name);
                }
            }
        }
        
        private async void CameraSelectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedCameraItem = e.AddedItems.FirstOrDefault().ToString();
            foreach (DeviceInformation item in _allVideoDevices)
            {
                if (string.Equals(item.Name, selectedCameraItem))
                {
                    _desiredDevice = item;
                    await StartDeviceAsync();
                }
            }
        }

        private async void TakePhotoButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("Taking photo");
                TakePhotoButton.IsEnabled = false;

                //store the captured image
                _photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync("capturedImage", CreationCollisionOption.ReplaceExisting);
                Debug.WriteLine("Create photo file successful");

                //create the properties to write 
                ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
                await _mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, _photoFile);

                TakePhotoButton.IsEnabled = true;
                Debug.WriteLine("Photo taken");

                //map the captured image as Bitmap image to the right column
                ImageElement.Source = await OpenImageAsBitmapAsync(_photoFile);
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
                TakePhotoButton.IsEnabled = true;
            }
        }

        private async void GetDetailsButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            GetDetailsErrorTextBlock.Text = string.Empty;

            if (_photoFile != null)
            {
                using (IRandomAccessStream stream = await _photoFile.OpenAsync(FileAccessMode.Read))
                {
                    // Create image decoder.
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                    // Load bitmap.
                    SoftwareBitmap bitmap = await decoder.GetSoftwareBitmapAsync();

                    // Extract text from image.
                    OcrResult result = await _ocrEngine.RecognizeAsync(bitmap);
                    if (string.IsNullOrEmpty(result.Text))
                    {
                        GetDetailsErrorTextBlock.Text = "Text not Recognizable try again!";
                        Debug.WriteLine("The Text is not recognizable.");
                    }
                    else
                    {
                        Debug.WriteLine(result.Text);

                        //extract the details
                        ApplyPatternMatching(result);
                    }
                }
            }
        }

#region Camera_Methods
        private async Task StartDeviceAsync()
        {
            if (_desiredDevice != null)
            {
                try
                {
                    Debug.WriteLine("Starting device");
                    _mediaCapture = new MediaCapture();

                    //initialize the selected device
                    await _mediaCapture.InitializeAsync(
                        new MediaCaptureInitializationSettings
                        {
                            VideoDeviceId = _desiredDevice.Id
                        });
                    
                    // if you have a valid camera then enable the photo button and start the preview window display
                    if (_mediaCapture.MediaCaptureSettings.VideoDeviceId != string.Empty
                        && _mediaCapture.MediaCaptureSettings.AudioDeviceId != string.Empty)
                    {
                        TakePhotoButton.IsEnabled = true;
                        Debug.WriteLine("Device initialized successful");
                        await StartPreviewAsync();
                    }
                    else
                    {
                        TakePhotoButton.IsEnabled = false;
                        Debug.WriteLine("Error - No VideoDevice/AudioDevice Found");
                    }
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception);
                }
            }
        }

        private async Task StartPreviewAsync()
        {
            try
            {
                Debug.WriteLine("Starting preview");
                //set the source to the camera feed
                PreviewElement.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();

                Debug.WriteLine("Start preview successful");
            }
            catch (Exception exception)
            {
                PreviewElement.Source = null;
                Debug.WriteLine(exception);
            }
        }
        #endregion

#region OCR_Methods
        private void ApplyPatternMatching(OcrResult ocrResult)
        {
            Contact contact = new Contact();
            //set the picture
            contact.SourceDisplayPicture = _photoFile;
            // this method uses an action that will run as a 'callback' for the method
            // more info here https://msdn.microsoft.com/en-us/library/018hxwa8(v=vs.110).aspx
            RepeatForOcrWords(ocrResult, (result, word) =>
            {
                bool isNumber = false;
                //check the recognized type and then add the type to the contact 
                switch (CardRecognizer.Recognize(word.Text))
                {
                    case RecognitionType.Other:
                        break;
                    case RecognitionType.Email:
                        contact.Emails.Add(new ContactEmail() { Address = word.Text });
                        break;
                    case RecognitionType.Name:
                        contact.FirstName = word.Text;
                        break;
                    case RecognitionType.Number:
                        isNumber = true;
                        //NOTE: Phone numbers are not as easy to validate because OCR results splits the numbers if they contain spaces.
                        _phoneNumber += word.Text;
                        RecognitionType type = CardRecognizer.Recognize(_phoneNumber);
                        if (type == RecognitionType.PhoneNumber)
                        {
                            contact.Phones.Add(new ContactPhone() { Number = _phoneNumber });
                        }
                        break;
                    case RecognitionType.WebPage:
                        try
                        {
                            contact.Websites.Add(new ContactWebsite() { Uri = new Uri(word.Text) });
                        }
                        catch (Exception)
                        {
                            Debug.WriteLine("OCR Result cannot be converted to a URI");
                        }
                        break;
                    default:
                        break;
                }

                //Encounted a word or a value other than a number. 
                //If we havent validated as a phone number at this stage it is clearly not a phone number so clear the string
                if (!isNumber)
                {
                    _phoneNumber = string.Empty;
                }
            });

            if (!contact.Phones.Any()) //contact must have either a phone or email when calling ContactManager.ShowContactCard.
            {
                if (!contact.Emails.Any())
                {
                    Debug.WriteLine("Contact must have phone or email info.");

                    return;
                }
            }

            Rect rect = GetElementRect(GetDetailsButton);
            ContactManager.ShowContactCard(contact, rect, Windows.UI.Popups.Placement.Default);
        }

        private void RepeatForOcrWords(OcrResult ocrResult, Action<OcrResult, OcrWord> repeater)
        {
            if (ocrResult.Lines != null)
            {
                foreach (OcrLine line in ocrResult.Lines)
                {
                    foreach (OcrWord word in line.Words)
                    {
                        //call the action method 
                        repeater(ocrResult, word);
                    }
                }
            }
        }

#endregion

#region HelperMethods
        //open an image file as a bitmapimage object 
        private async Task<BitmapImage> OpenImageAsBitmapAsync(StorageFile file)
        {
            IRandomAccessStreamWithContentType stream = await file.OpenReadAsync();
            BitmapImage bmpImg = new BitmapImage();
            bmpImg.SetSource(stream);

            return bmpImg;
        }

        //get the bounding rect of an element relative to 0,0
        private static Rect GetElementRect(FrameworkElement element)
        {
            //get the element point to open the window at the correct point
            GeneralTransform transform = element.TransformToVisual(null);
            Point point = transform.TransformPoint(new Point());

            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }

#endregion
    }
}
