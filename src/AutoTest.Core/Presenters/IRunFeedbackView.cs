﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoTest.Core.Messaging;

namespace AutoTest.Core.Presenters
{
    public interface IRunFeedbackView
    {
        void RecievingBuildMessage(BuildRunMessage runMessage);
    }
}