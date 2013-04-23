using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using FlickrNet;

namespace FlickrKinectPhotoFun
{
    public class flikrCustomCursorEventArgs : EventArgs
    {
        public String resultMessage = "";

        public flikrCustomCursorEventArgs(string _resultMessage)
        {
            resultMessage = _resultMessage;
        }
    }

    public class FlickrSearch : DispatcherObject
    {
        // The event delegate
        public delegate void onFlikrResults(object sender, flikrCustomCursorEventArgs e);
        // The event
        public event onFlikrResults flikrSearchResultEvent;

        public Flickr _fLib;
        public PhotoCollection _mostRecentPhotos;
  
        private string ApiKey = Properties.Settings.Default.flickrAPIKey;
        private string SharedSecret = Properties.Settings.Default.flickrSharedSecret;

        public FlickrSearch()
        {
            _fLib = new Flickr(ApiKey, SharedSecret);
        }

        public void newSearch(string _searchTerms)
        {
            PhotoSearchOptions searchOptions = new PhotoSearchOptions();
            searchOptions.Extras = PhotoSearchExtras.AllUrls | PhotoSearchExtras.Description | PhotoSearchExtras.OwnerName;
            searchOptions.SortOrder = PhotoSearchSortOrder.Relevance;
            searchOptions.Tags = _searchTerms;

            _fLib.PhotosSearchAsync(searchOptions, r =>
            {

                    this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
                    {
                            if(flikrSearchResultEvent != null) {

                               flikrCustomCursorEventArgs fResultMsg;

                                if (r.Error != null)
                                {
                                    fResultMsg = new flikrCustomCursorEventArgs(r.Error.Message);
                                    flikrSearchResultEvent(this, fResultMsg);
                                } else {
                                   _mostRecentPhotos = r.Result;
                                   fResultMsg = new flikrCustomCursorEventArgs("");
                                   fResultMsg.resultMessage = "Search complete. Found " + _mostRecentPhotos.Count + " photos.";
                                   flikrSearchResultEvent(this, fResultMsg);
                                }
                            }
                    }));

            });

        }

    }
}
