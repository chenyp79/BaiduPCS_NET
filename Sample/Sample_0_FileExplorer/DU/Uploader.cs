﻿using System;
using System.IO;

using BaiduPCS_NET;

namespace FileExplorer
{

    public class Uploader : ICancellable, IProgressable
    {
        public BaiduPCS pcs { get; protected set; }
        public string from { get; set; }
        public string to { get; set; }
        public long DoneSize { get; protected set; }
        public bool Success { get; protected set; }
        public bool IsCancelled { get; protected set; }
        public Exception Error { get; protected set; }
        public bool Uploading { get; protected set; }
        public PcsFileInfo Result { get; protected set; }
        public object State { get; set; }

        public event EventHandler<CompletedEventArgs> Completed;
        public event EventHandler<ProgressEventArgs> Progress;
        public event EventHandler<StateFileNameDecideEventArgs> StateFileNameDecide;

        public Uploader(BaiduPCS pcs, string from, string to)
        {
            this.pcs = pcs;
            this.from = from;
            this.to = to;
        }

        public virtual void Upload()
        {
            if (Uploading)
                throw new Exception("Can't upload, since the previous upload is not complete.");
            DoneSize = 0;
            Success = false;
            IsCancelled = false;
            Error = null;
            Uploading = true;
            try
            {
                BaiduPCS pcs = this.pcs.clone();
                pcs.Progress += onProgress;
                Result = pcs.upload(to, from, false);
                if (!Result.IsEmpty)
                {
                    Success = true;
                    IsCancelled = false;
                }
                else if (IsCancelled)
                {
                    Success = false;
                    IsCancelled = true;
                }
                else
                {
                    Success = false;
                    IsCancelled = false;
                    if (Error == null)
                        Error = new Exception(pcs.getError());
                }
            }
            catch (Exception ex)
            {
                Success = false;
                IsCancelled = false;
                Error = ex;
            }
            Uploading = false;
            fireCompleted(new CompletedEventArgs(Success, IsCancelled, Error));
        }

        public virtual void Cancel()
        {
            IsCancelled = true;
        }

        private int onProgress(BaiduPCS sender, double dltotal, double dlnow, double ultotal, double ulnow, object userdata)
        {
            ProgressEventArgs args = new ProgressEventArgs((long)ulnow, (long)ultotal);
            fireProgress(args);
            if (args.Cancel)
            {
                IsCancelled = true;
                return -1;
            }
            return 0;
        }

        protected virtual void fireProgress(ProgressEventArgs args)
        {
            if (Progress != null)
                Progress(this, args);
        }

        protected virtual void fireCompleted(CompletedEventArgs args)
        {
            if (Completed != null)
                Completed(this, args);
        }

        protected virtual void fireStateFileNameDecide(StateFileNameDecideEventArgs args)
        {
            if (StateFileNameDecide != null)
                StateFileNameDecide(this, args);
        }
    }
}
