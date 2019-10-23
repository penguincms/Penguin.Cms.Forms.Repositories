﻿using Penguin.Cms.Repositories;
using Penguin.Messaging.Core;
using Penguin.Persistence.Abstractions.Interfaces;
using Penguin.Security.Abstractions.Extensions;
using Penguin.Security.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Penguin.Cms.Forms.Repositories
{
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix")]
    public class FormSubmissionRepository : EntityRepository<SubmittedForm>
    {
        protected ISecurityProvider<SubmittedForm> SecurityProvider { get; set; }

        public FormSubmissionRepository(IPersistenceContext<SubmittedForm> dbContext, ISecurityProvider<SubmittedForm> securityProvider = null, MessageBus messageBus = null) : base(dbContext, messageBus)
        {
            SecurityProvider = securityProvider;
        }

        public List<SubmittedForm> GetByOwner(Guid owner) => this.Where(j => j.Owner == owner).ToList().Where(f => SecurityProvider.TryCheckAccess(f)).ToList();
    }
}