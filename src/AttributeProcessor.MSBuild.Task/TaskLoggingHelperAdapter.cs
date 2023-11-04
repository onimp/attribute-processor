using AttributeProcessor.Core.Logging;
using Microsoft.Build.Utilities;

namespace AttributeProcessor.MSBuild.Task;

public class TaskLoggingHelperAdapter : ILogger {

    private readonly TaskLoggingHelper helper;

    public TaskLoggingHelperAdapter(TaskLoggingHelper helper) {
        this.helper = helper;
    }

    public void Error(string message) => helper.LogError(message);

}
