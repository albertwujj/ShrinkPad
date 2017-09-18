

### Description: ###
A Windows 10 app that allows the user to draw strokes, but with a catch: 
The strokes will intelligently resize themselves by a certain percentage after a 
certain amount of time has passed without pen input, or after one of several other conditions. 
Developed solely as a prototype app to encourage implementation in Microsoft OneNote and other pen input applications.

### Conception: ###
I always found myself needing more writing space when doing homework/taking notes with the stylus in 
OneNote. Often, I would manually scale down my own writing, which is a tedious process in OneNote. I thought, what if there was a feature where your writing would scale down automatically?



### List of all features: ###

 * Automatically resize all strokes not already resized, after a certain time period of no pen contact
     * Resize percentage, and time period can be set using a slider

 * Resizes around a certain reference point: the top left corner of the set of all strokes 
 to be resized

 * Intelligent resizing: If a new stroke is not within the (un-resized) bounding rectangle of the last resized set of strokes, 
 but close enough to the last point drawn, keep the previous reference point
     * This makes the app much smoother, as the condition being met usually means the user is 
 continuing the same "flow" of writing
 
 * If the new stroke is very far from the last point drawn, automatically resize

 * Height Option: If the height option is selected, resize all strokes to the same median height as the first set of strokes resized

 * Also press R or right click to resize

 * Press Y to undo shrinking the last stroke

 * Press U to delete the last stroke

 * Upload a file as a background image

 * Change pen color

