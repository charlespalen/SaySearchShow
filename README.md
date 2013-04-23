Introduction
===============================================
This is an application that uses voice recognition to search Flickr photos, then randomly display them in an artistic way if any are found. 
It is a full screen application designed for projections or large format monitors. This application supports the Microsoft Kinect, but 
it is not required.

The objective of this piece was to provide a fun and inspiring application that could be run in an office or public space on a projector or 
large format display to essentially dish out inspiration with a hint of interactivity. Due to the vast creativity of all of the photos 
on the Flickr platform, it really has been inspiring to see what shows up at times.

How to Use
--------------------------------------------------
In order to use the application, just download the "SaySearchShow.msi" application and install it.
If you don't have a Kinect For Windows, you can use a standard PC microphone. Just make sure your recording levels
are high enough.

If you do have a Kinect For Windows, you should notice an increase in the voice recognition capability of the application.
This version was build with the Kinect 1.7 SDK. You may need to install the 1.7 Kindect For Windows runtime.

In order to search, start by saying the phrase "Kinect Show" or "Xbox Show" followed by your wildcard photo search terms.

The application will use whatever screen you open it on as the primary "full screen" display.  If you don't want it to run on your
primary monitor, just create a shortcut and open it on your second monitor.

Configuration Options
--------------------------------------------------
There are a few configuration options available that you can change by editing the configuration file.
ApplicationInstallDirectory/SaySearchShow.exe.config

Text Fade off Delay in Seconds. In the xml below it is set to 5 seconds. Change this value and run the application.
```xml
<setting name="textDelayTimeinSec" serializeAs="String">
	<value>5</value>
</setting>
```
Minimum randomly selected time to show a photo in seconds. In the example below it is set to 20 seconds.
```xml
<setting name="photoDisplayTimeMinSec" serializeAs="String">
	<value>20</value>
</setting>
```
Maximum randomly selected time to show a photo in seconds. In the example below it is set to 50 seconds.
```xml
<setting name="photoDisplayTimeMaxSec" serializeAs="String">
	<value>50</value>
</setting>
```
A maximum timeout in seconds given for the FlickrNet library to downlaod an image before we move on to the next.
During testing timeouts would happen even on excellent client bandwidth.
```xml
<setting name="maximumDownloadTimeoutSec" serializeAs="String">
	<value>60</value>
</setting>
```
Images are scaled by this much before they are displayed.
```xml
<setting name="imageScaleMultiplier" serializeAs="String">
	<value>1.5</value>
</setting>
```
There are two other settings that you can use to change the API key the application uses.

Development
--------------------------------------------------
This project was created using Visual C# 2010 Express. You should be able to dig in using visual studio or
one of the other express development platforms.

External Links
--------------------------------------------------
[Say Search Show Flickr Application Page](http://www.flickr.com/services/apps/72157631721003670/)
[Say Search Show Page on Technogumbo](http://www.technogumbo.com/projects/say-search-show/index.php)
[Flickr Net Project on Codeplex](http://flickrnet.codeplex.com/)
[Microsoft Speech Platform](http://msdn.microsoft.com/en-us/library/hh361572.aspx)
	
License
--------------------------------------------------
This project is licensed under the freeBSD license. See LICENSE.txt

FlickrNet is licensed under the LGPL license. See SaySearchShow/lib/LICENSE.txt