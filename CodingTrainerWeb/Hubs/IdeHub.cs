﻿using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using CodingTrainer.CSharpRunner.CodeHost;
using CodingTrainer.CodingTrainerWeb.Hubs.Helpers;
using System.Collections.Concurrent;
using System.Threading;

namespace CodingTrainer.CodingTrainerWeb.Hubs
{
    public class IdeHub : Hub<IIdeHubClient>, IIdeHubServer
    {
        private IIdeServices ideServices;
        public IdeHub(IIdeServices ideServices)
        {
            this.ideServices = ideServices;
        }
        public IdeHub()
            :this(new IdeServices())
        { }

        private static ConcurrentDictionary<string, CancellationTokenSource> inProgress = new ConcurrentDictionary<string, CancellationTokenSource>();

        public async Task Validate(string code, int generation)
        {
            // If this connection already has a validation in progress, cancel it
            if (inProgress.TryRemove(Context.ConnectionId, out var prevCts))
            {
                prevCts.Cancel();
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            Task t = Task.Run(async () => await ValidateCancellable(code, generation, cts.Token), cts.Token);
            inProgress[Context.ConnectionId] = cts;
            await t;
        }

        private async Task ValidateCancellable(string code, int generation, CancellationToken token)
        {
            var diags = await ideServices.GetDiagnostics(code);

            // If cancelled, then don't bother sending details back to the client
            token.ThrowIfCancellationRequested();

            inProgress.TryRemove(Context.ConnectionId, out var ignore);
            if (diags == null)
            {
                Clients.Caller.CompilerError(null, generation);
            }
            else
            {
                Clients.Caller.CompilerError(CompilerError.ArrayFromDiagnostics(diags), generation);
            }
        }
    }
}