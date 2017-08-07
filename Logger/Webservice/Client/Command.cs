using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Deserializers;

namespace Logger.Webservice
{
    partial class Client
    {
        public List<Command> GetCommands()
        {
            return Execute<List<Command>>(new CommandsRequest());
        }

        public Task<List<Command>> GetCommandsAsync(CancellationToken token)
        {
            return ExecuteAsync<List<Command>>(new CommandsRequest(), token);
        }

        public void CompleteCommand(string commandID, string error)
        {
            Execute(new CompleteCommandRequest(commandID, error));
        }

        public Task CompleteCommandAsync(string commandID, string error, CancellationToken token)
        {
            return ExecuteAsync(new CompleteCommandRequest(commandID, error), token);
        }
    }

    class CommandsRequest : RetrieveRequest
    {
        public CommandsRequest() : base("command/") { }
    }

    class CompleteCommandRequest : SubmitRequest<string>
    {
        public CompleteCommandRequest(string commandID, string error) : base($"command/{ commandID }/", error)
        {
        }

        protected override object ToJSONShape(string error)
        {
            return new
            {
                completed = true,
                error = error
            };
        }
    }

    class Command
    {
        [DeserializeAs(Name = "type")]
        public string Type { get; set; }

        [DeserializeAs(Name = "parameters")]
        public string Parameters { get; set; }

        [DeserializeAs(Name = "id")]
        public string ID { get; set; }
    }
}
