// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Linq;
using ReusableLibraryCode;

namespace ReusableUIComponents.Dialogs
{
    /// <summary>
    /// Used by the RDMP to tell you about something that went wrong.  You can select bits of the message text and copy them with Ctrl+C or select 'Copy to Clipboard' to copy all the
    /// message text in one go.  Clicking 'View Exception' will launch a ExceptionViewerStackTraceWithHyperlinks for viewing the location of the error in the codebase (including viewing
    /// the source code at the point of the error).
    /// </summary>
    public class ExceptionViewer : WideMessageBox
    {
        private readonly Exception _exception;
        
        private ExceptionViewer(string title, string message, Exception exception):base(new WideMessageBoxArgs(title,message,exception.StackTrace??Environment.StackTrace,null,WideMessageBoxTheme.Exception))
        {
            _exception = exception;

            var aggregateException = _exception as AggregateException;

            if (aggregateException != null)
            {
                _exception = aggregateException.Flatten();

                if(aggregateException.InnerExceptions.Count == 1)
                    _exception = aggregateException.InnerExceptions[0];
            }
        }
        
        public static void Show(Exception exception, bool isModalDialog = true)
        {
            var longMessage = "";

            if(exception.InnerException != null)
                longMessage = ExceptionHelper.ExceptionToListOfInnerMessages(exception.InnerException );

            ExceptionViewer ev;
            if (longMessage == "")
                ev = new ExceptionViewer(exception.GetType().Name,exception.Message, exception);
            else
                ev = new ExceptionViewer(exception.Message,longMessage, exception);

            if (isModalDialog)
                ev.ShowDialog();
            else
                ev.Show();
        }
        public static void Show(string message, Exception exception, bool isModalDialog = true)
        {
            var longMessage = "";

            //if the API user is not being silly and passing a message that is the exception anyway!
            if (message.StartsWith(exception.Message))
            {
                if (exception.InnerException != null)
                    longMessage = ExceptionHelper.ExceptionToListOfInnerMessages(exception.InnerException);
            }
            else
                longMessage = ExceptionHelper.ExceptionToListOfInnerMessages(exception);

            if (message.Trim().Contains("\n"))
            {
                var split = message.Trim().Split('\n');
                message = split[0];

                longMessage = string.Join(Environment.NewLine,split.Skip(1)) + Environment.NewLine + Environment.NewLine + longMessage;
            }

            ExceptionViewer ev = new ExceptionViewer(message,longMessage,exception);

            if(isModalDialog)
                ev.ShowDialog();
            else
                ev.Show();
        }

        protected override void OnViewStackTrace()
        {
            if (ExceptionViewerStackTraceWithHyperlinks.IsSourceCodeAvailable(_exception))
                ExceptionViewerStackTraceWithHyperlinks.Show(_exception);
            else
                base.OnViewStackTrace();
        }
    }
}
