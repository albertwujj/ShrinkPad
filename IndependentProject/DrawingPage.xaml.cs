using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Priority_Queue;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238



/* LIST OF ALL FEATURES
    Automatically resize all strokes not already resized, after a certain time period of no pen contact, based on a slider
    Resizing is intelligent, app resizes around a certain reference point: the top left corner of the set of all strokes to be resized
    If a new stroke after resize is not within the (un-resized) bounding rectangle of the last resized set of strokes, but close enough to the last point, keep the previous reference point
        This makes the app much more smooth
    Also press R or right click to resize
    Resize by a certain percentage based on a slider
    Press Y to undo shrinking the last stroke
    Press U to delete the last stroke
    Upload a file as a background image
    Change pen color
    A pseudo-eraser (white pen color, cannot resize when eraser is selected)
    If height option is selected, resize all strokes to the same median height as the first set of strokes resized
*/





namespace IndependentProject
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DrawingPage : Page
    {
        Boolean tempSelection = false;

        private DateTime lastStroke = DateTime.UtcNow;

        private int imageWidth = 100;
        //timer
        private int A = 2000;

        //max distance of new point after a resize from the last point required to keep 
        //the previous reference point
        private int D = 40000;


        //distance required for a resize based on touch
        //private int resizeTouch = 100000;


        private HashSet<InkStroke> curr = new HashSet<InkStroke>();


        //bounding rect of curr
        private double leftmostX = Double.MaxValue;
        private double highestY = Double.MaxValue;
        private double rightmostX = Double.MinValue;
        private double lowestY = Double.MinValue;

        //all the previous resized StrokeSets
        private StrokeSet prev;
        private Stack<StrokeSet> prevs = new Stack<StrokeSet>();

        private Dictionary<InkStroke, StrokeSet> strokeToSet = new Dictionary<InkStroke, StrokeSet>();

        //firstPress of Curr
        private Boolean notPressedYet = true;
        private Point firstP = new Point(0, 0);
        private DateTime firstPTime = DateTime.UtcNow;

        private bool tooClose = false;


        private Point currLastTouch = new Point(0, 0);


        //lastTouch
        private Point lastTouch;

        private int lastStrokeId = 0;

        //pen stroke info
        private Color penColor;
        private int eraserSize = 100;
        private bool eraserOn = false;
        private double penSize = 1;
        private double prevAreaPerStroke = 0;

        private double resizePercentage = 0.4;


        private StorageFile currentFile;

        private SharedOptions sO;

        private int currNumPoints = 0;
        private Boolean rJustChanged;

        //fields used to calculate running median of height of strokes
        //Used a NuGet package for the class found from this site:https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp/wiki/Getting-Started
        //Date used: 6/12/17
        private SimplePriorityQueue<Double> medMinHeap = new SimplePriorityQueue<double>();
        private SimplePriorityQueue<Double> medMaxHeap = new SimplePriorityQueue<double>();

        public DrawingPage()
        {
            this.InitializeComponent();
            inkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;

            //event listeners

            //check to see if press is a selection of prev first
            //canvasGrid.PointerPressed += CanvasGrid_PointerPressed;
            //canvasGrid.PointerMoved += CanvasGrid_PointerMoved;
            //check to know when pen is no longer lifted, and to resize if new stroke is far enough from old
            //inkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInput_StrokeStarted;

            //keep track of last point of  each stroke
            //inkCanvas.InkPresenter.StrokeInput.StrokeEnded += StrokeInput_StrokeEnded;

            //keep track of the bounding rectangle of curr
            //if it is the first stroke in curr, check if it is close but not within prev's boundaries
            //inkCanvas.InkPresenter.StrokesCollected += inkPresenter_StrokesCollected;

            //implement right-click to resize
            //inkCanvas.RightTapped += InkCanvas_RightTapped;
            //keyboard input
            CoreWindow.GetForCurrentThread().KeyDown += DrawingPage_KeyDown;

            //canvasGrid.PointerPressed += CanvasGrid_PointerPressed;
            //canvasGrid.PointerMoved += CanvasGrid_PointerMoved;
            inkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInput_StrokeStarted;
            inkCanvas.InkPresenter.StrokeInput.StrokeEnded += StrokeInput_StrokeEnded;
            inkCanvas.InkPresenter.StrokesCollected += inkPresenter_StrokesCollected;
            inkCanvas.RightTapped += InkCanvas_RightTapped;

            //change starting pen size
            InkDrawingAttributes drawingAttributes =
                    inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
            drawingAttributes.Size = new Size(penSize, penSize);
            inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);


        }


        //logic for shrinking all strokes not currently shrunk
        private void shrink()
        {
            if (!notPressedYet)
            {



                int numStrokes = 0;

                Point currReferencePoint = new Point();

                //keep previous reference point if first stroke of curr was close to last stroke of prev, and 5000 ms have passed.
                if (prev != null && tooClose && DateTime.UtcNow - prev.endTime < TimeSpan.FromMilliseconds(5000))
                {
                    currReferencePoint.X = prev.referencePoint.X;
                    currReferencePoint.Y = prev.referencePoint.Y;
                }
                else
                {
                    currReferencePoint.X = leftmostX;
                    currReferencePoint.Y = highestY;
                }
                prev = new StrokeSet();


                foreach (InkStroke stroke in curr)
                {

                    if (sO.HeightOption)
                    {
                        //if there is no prev, or if ResizeChooser slider was just changed, resize to a certain percentage (controlled by Slider), otherwise, resize to previous  average density.
                        if (rJustChanged || prevs.Count == 0)
                        {
                            stroke.PointTransform = Matrix3x2.CreateScale((float)resizePercentage, currReferencePoint.ToVector2());
                            rJustChanged = false;
                            prev.resizeFactor = resizePercentage;
                            double currSHeight = medMinHeap.First;
                            if (medMinHeap.Count == medMaxHeap.Count)
                            {
                                currSHeight = (medMinHeap.First + medMaxHeap.First) / 2;
                            }
                            prev.SHeight = currSHeight;

                            
                        }

                        //else resize to previous height/density
                        else
                        {   /* obsolete density code
                            if (sO.DensityOption)
                            {
                                //prevS meaning the last prev on the stack, not the one that is being created right now during this resize
                                StrokeSet prevS = prevs.Peek();
                                double prevSArea = prevS.boundingRect.Height * prevS.boundingRect.Width * prevS.resizeFactor * prevS.resizeFactor;
                                double currArea = (highestY - lowestY) * (leftmostX - rightmostX);
                                double factor = (prevSArea / prevS.numPoints) / (currArea / currNumPoints);
                                stroke.PointTransform = Matrix3x2.CreateScale((float)factor, currReferencePoint.ToVector2());
                                prev.resizeFactor = factor;
                            } */

StrokeSet prevS = prevs.Peek();
                            //double prevSHeight = prevS.boundingRect.Height * prevS.resizeFactor;
                            double prevSHeight = prevS.SHeight * prevS.resizeFactor;
                           
                            double currSHeight = medMinHeap.First;
                            if (medMinHeap.Count == medMaxHeap.Count)
                            {
                                currSHeight = (medMinHeap.First + medMaxHeap.First) / 2;
                            }

                            double factor = prevSHeight / currSHeight;
                            stroke.PointTransform = Matrix3x2.CreateScale((float)factor, currReferencePoint.ToVector2());
                            prev.resizeFactor = factor;
                            prev.SHeight = currSHeight;

                            
                            
                        }
                        
                    }
                    else
                    {
                        stroke.PointTransform = Matrix3x2.CreateScale((float)resizePercentage, currReferencePoint.ToVector2());
                        prev.resizeFactor = resizePercentage;

                    }
                    strokeToSet.Add(stroke, prev);
                    numStrokes++;
                }

                prev.numStrokes = numStrokes;

                prev.boundingRect = new Rect(leftmostX, highestY, rightmostX - leftmostX, lowestY - highestY);

                prev.referencePoint = currReferencePoint;
                prev.endTime = DateTime.UtcNow;

                prev.numPoints = currNumPoints;
                currNumPoints = 0;

                //resets curr
                prev.strokes = curr;
                curr = new HashSet<InkStroke>();



                //resets boundaries
                leftmostX = Double.MaxValue;
                highestY = Double.MaxValue;
                rightmostX = Double.MinValue;
                lowestY = Double.MinValue;
                //resets checker for first press
                notPressedYet = true;
                //resets checker for tooClose
                tooClose = false;
                lastStrokeId = 0;

               
                if(sO.HeightOption)
                {
                    //reset median trackers
                    medMaxHeap = new SimplePriorityQueue<double>();
                    medMinHeap = new SimplePriorityQueue<double>();
                }

                prev.lastTouch = currLastTouch;

                prevs.Push(prev);
            }
        }

        private void DrawingPage_KeyDown(CoreWindow sender, KeyEventArgs e)
        {
            if (e.VirtualKey == Windows.System.VirtualKey.R)
            {
                shrink();
            }
            if (e.VirtualKey == Windows.System.VirtualKey.Y)
            {
                undoShrink();
            }
            if (e.VirtualKey == Windows.System.VirtualKey.U)
            {
                undo();
            }
        }

        //undo the last shrink
        private void undoShrink()
        {
            if (prevs.Count != 0)
            {
                StrokeSet shrinkThis = prevs.Pop();
                foreach (InkStroke stroke in shrinkThis.strokes)
                {
                    stroke.PointTransform = Matrix3x2.Multiply(stroke.PointTransform, Matrix3x2.CreateScale((float)((float)1 / shrinkThis.resizeFactor), shrinkThis.referencePoint.ToVector2()));
                }
            }
        }

        //delete last stroke
        private void undo()
        {
            if (inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Count != 0)
            {
                inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Last<InkStroke>().Selected = true;
                inkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
            }
        }

        private void StrokeInput_StrokeEnded(InkStrokeInput sender, PointerEventArgs e)
        {
            lastTouch = e.CurrentPoint.Position;
        }

        private void InkCanvas_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            shrink();
        }

        private void StrokeInput_StrokeStarted(InkStrokeInput sender, PointerEventArgs e)
        {
            lastStrokeId++;
            Point p = e.CurrentPoint.Position;
            /*
            if ((p.X - lastTouch.X) * (p.X - lastTouch.X) + (p.Y - lastTouch.Y) * (p.Y - lastTouch.Y) > resizeTouch)
            {
                resize();
            }
            */

        }

        private void inkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs e)
        {
            if (!eraserOn)
            {

                //keeps track of bounding rectangles
                foreach (InkStroke stroke in e.Strokes)
                {
                    if (stroke.BoundingRect.X < leftmostX)
                    {
                        leftmostX = stroke.BoundingRect.X;
                    }
                    if (stroke.BoundingRect.Y < highestY)
                    {
                        highestY = stroke.BoundingRect.Y;
                    }
                    if (stroke.BoundingRect.X + stroke.BoundingRect.Width > rightmostX)
                    {
                        rightmostX = stroke.BoundingRect.X + stroke.BoundingRect.Width;
                    }
                    if (stroke.BoundingRect.Y + stroke.BoundingRect.Height > lowestY)
                    {
                        lowestY = stroke.BoundingRect.Y + stroke.BoundingRect.Height;
                    }
                    curr.Add(stroke);
                    double minY = double.MaxValue;
                    double maxY = double.MinValue;
                    foreach (InkPoint point in stroke.GetInkPoints())
                    {
                        if (point.Position.Y > maxY)
                        {
                            maxY = point.Position.Y;
                        }
                        if (point.Position.Y < minY)
                        {
                            minY = point.Position.Y;
                        }
                        currNumPoints++;
                    }

                    //keeps a maxHeap representing the smaller half of the set of heights, and a minHeap representing the larger half,
                    //and keeps their sizes balanced so the median is always either the minHeap's min height, or the average of the
                    //maxHeap's max height and minHeaps min height 
                    double height = maxY - minY;
                    if (medMaxHeap.Count == 0)
                    {
                        if (medMinHeap.Count == 0)
                        {
                            medMinHeap.Enqueue(height, (float)height);
                        }
                        else
                        {
                            if (height >= medMinHeap.First)
                            {
                                medMinHeap.Enqueue(height, (float)height);
                            }
                            else
                            {
                                medMaxHeap.Enqueue(height, (float)height);
                            }
                        }
                    }
                    else if (medMaxHeap.First >= height)
                    {
                        medMaxHeap.Enqueue(height, (float)height);

                    }
                    else
                    {
                        medMinHeap.Enqueue(height, (float)height);
                    }

                    if (medMinHeap.Count > medMaxHeap.Count + 1)
                    {
                        double moved = medMinHeap.Dequeue();
                        medMaxHeap.Enqueue(moved, (float)moved);

                    }
                    if (medMaxHeap.Count > medMinHeap.Count)
                    {
                        double moved = medMaxHeap.Dequeue();
                        medMinHeap.Enqueue(moved, (float)moved);
                    }
                }
                currLastTouch = e.Strokes.Last<InkStroke>().GetInkPoints().Last<InkPoint>().Position;


                //if should keep old reference point based on first point
                //add only within a certain time
                if (notPressedYet == true)
                {

                    //gets the first inkPoint, compares it to the prev inkpoint
                    firstP = e.Strokes.First<InkStroke>().GetInkPoints().First<InkPoint>().Position;

                    notPressedYet = false;

                    double offset = Math.Sqrt(prevAreaPerStroke) / 5;

                    //if first is not within prev* (with certain barrier based on average stroke size)
                    //*should make more than prev, based on certain time

                    if (prev != null && !(firstP.X > prev.boundingRect.X + offset && firstP.X < prev.boundingRect.X + prev.boundingRect.Width - offset && firstP.Y > prev.boundingRect.Y + offset && firstP.Y < prev.boundingRect.Y + prev.boundingRect.Height - offset))
                    {
                        //if first is close to prev last touch
                        if ((firstP.X - prev.lastTouch.X) * (firstP.X - prev.lastTouch.X) + (firstP.Y - prev.lastTouch.Y) * (firstP.Y - prev.lastTouch.Y) < D)
                        {
                            tooClose = true;
                        }

                    }
                }


                //sets a timer for x seconds after stroke is released
                lastStroke = DateTime.UtcNow;
                lastStrokeId++;
                int strokeId = lastStrokeId;

                TimeSpan delay = TimeSpan.FromMilliseconds(A);
                ThreadPoolTimer timer = ThreadPoolTimer.CreateTimer((t) => { handlerWorker(t, strokeId); }, delay);


            }
        }

        //if the last stroke is still the sender of the x second timer, that means x seconds have passed without a new stroke, 
        //so shrink
        private void handlerWorker(ThreadPoolTimer timer, int sender)
        {

            if (sender == lastStrokeId)
            {
                shrink();
            }
        }

        //changing percentage of shrink
        private void ReSizeChooser_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Slider slider = sender as Slider;
            if (slider != null)
            {
                resizePercentage = slider.Value / 100;
            }
            rJustChanged = true;
        }

        // Update ink stroke color for new strokes.
        // code for method from: https://docs.microsoft.com/en-us/windows/uwp/input-and-devices/pen-and-stylus-interactions Date used: 4/2017
        private void OnPenColorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (inkCanvas != null)
            {
                InkDrawingAttributes drawingAttributes =
                    inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
                string value = ((ComboBoxItem)PenColor.SelectedItem).Content.ToString();
                switch (value)
                {
                    case "Black":
                        penColor = Colors.Black;
                        if (!eraserOn)
                        {
                            drawingAttributes.Color = penColor;
                        }
                        break;
                    case "Red":
                        penColor = Colors.Red;
                        if (!eraserOn)
                        {
                            drawingAttributes.Color = penColor;
                        }
                        break;
                    case "Blue":
                        penColor = Colors.Blue;
                        if (!eraserOn)
                        {
                            drawingAttributes.Color = penColor;
                        }
                        break;
                    case "Green":
                        penColor = Colors.Green;
                        if (!eraserOn)
                        {
                            drawingAttributes.Color = penColor;
                        }
                        break;
                    case "White":
                        penColor = Colors.White;
                        if (!eraserOn)
                        {
                            drawingAttributes.Color = penColor;
                        }
                        break;
                };
                inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
            }
        }

        //select pen or eraser
        //eraser is just white, does not work with background image
        private void PenOrEraser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (inkCanvas != null)
            {
                InkDrawingAttributes drawingAttributes =
                    inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
                string value = ((ComboBoxItem)PenOrEraser.SelectedItem).Content.ToString();
                switch (value)
                {
                    case "Pen":
                        eraserOn = false;
                        drawingAttributes.Color = penColor;
                        drawingAttributes.Size = new Size(penSize, penSize);
                        break;
                    case "Eraser":
                        eraserOn = true;
                        drawingAttributes.Color = Colors.White;
                        drawingAttributes.Size = new Size(eraserSize, eraserSize);
                        break;

                };
                inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
            }
        }

        //changing time to autoshrink
        private void TimerChooser_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            A = (int)e.NewValue * 1000;

        }

        //the class that keeps track of previously resized sets of strokes
        private class StrokeSet
        {
            public int numStrokes;
            public Rect boundingRect;
            public HashSet<InkStroke> strokes;
            public Point referencePoint;
            //public DateTime startTime;
            public DateTime endTime;
            public Point lastTouch;
            public double resizeFactor;
            public int numPoints;
            public double SHeight;
        }


        // allows user to choose background image file
        //code for method from: https://docs.microsoft.com/en-us/windows/uwp/files/quickstart-using-file-and-folder-pickers Date used: 4/2017
        private async void FileChooser_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".doc");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file
                //this.textBlock.Text = "Picked photo: " + file.Name;            
                if (file.FileType != ".docx")
                {
                    background.Source = await ImageUtils.StorageFileToBitmapImage(file, imageWidth);
                    currentFile = file;
                }
                /*
                else
                {

                }
                */
            }
            /*
            else
            {
                this.textBlock.Text = "Operation cancelled.";
            }
            */
        }

        //reads an image from a file
        //code from: http://windowsapptutorials.com/tips/storagefile/convert-storagefile-to-a-bitmapimage-in-universal-windows-apps/ Date used: 4/2017
        public class ImageUtils
        {
            public static double I;

            public static async Task<BitmapImage> StorageFileToBitmapImage(StorageFile savedStorageFile, int fileWidth)
            {

                using (IRandomAccessStream fileStream = await savedStorageFile.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    BitmapImage bitmapImage = new BitmapImage();

                    //bitmapImage.DecodePixelWidth = fileWidth;
                    await bitmapImage.SetSourceAsync(fileStream);
                    return bitmapImage;
                }
            }

        }

        //changing background image size
        private void sizeChooser_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (background != null)
            {
                background.Width = e.NewValue * 100;
            }
        }

        //page navigation logic
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            sO = (SharedOptions)e.Parameter;



            if (sO.HeightOption)
            {
                ReSizeChooser.Header = "Resize Height";
            }
        }
        private void AOButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AdvancedOptionsPage), sO);
        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage), sO);
        }

       /* idea did not make it into final project
        * 
       private void CanvasGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
       {
           Point p = e.GetCurrentPoint(inkCanvas).Position;
           if (tempSelection)
           {
               foreach (InkStroke stroke in prev.strokes)
               {
                   stroke.PointTransform = Matrix3x2.Multiply(stroke.PointTransform, Matrix3x2.CreateTranslation(p.ToVector2()));
               }
           }
       }
       */

       /* idea did not make it into final project
       private void CanvasGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
       {
           Point p = e.GetCurrentPoint(inkCanvas).Position;

           if (prev != null && p.X > prev.boundingRect.X && p.X < prev.boundingRect.X + prev.boundingRect.Width * prev.resizeFactor && p.Y > prev.boundingRect.Y && p.Y < prev.boundingRect.Y + prev.boundingRect.Height * prev.resizeFactor)
           {

               foreach (InkStroke stroke in prev.strokes)
               {
                   stroke.Selected = true;
               }
               tempSelection = true;
               e.Handled = true;
           }


       }
       */
    }
}
/*
For Use of Priority_Queue add-on, under MIT License:   

 Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

*/
