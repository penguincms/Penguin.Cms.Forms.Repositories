﻿using Penguin.Cms.Repositories;
using Penguin.Messaging.Core;
using Penguin.Messaging.Persistence.Messages;
using Penguin.Persistence.Abstractions.Attributes.Rendering;
using Penguin.Persistence.Abstractions.Interfaces;
using Penguin.Reflection;
using Penguin.Security.Abstractions.Extensions;
using Penguin.Security.Abstractions.Interfaces;
using System;
using System.Linq;
using System.Reflection;

namespace Penguin.Cms.Forms.Repositories
{
    public class FormRepository : AuditableEntityRepository<JsonForm>
    {
        private const string FORM_NAME_COLLISION_MESSAGE = "Can not save form with name that matches display name of form class";
        protected ISecurityProvider<Form> SecurityProvider { get; set; }

        public FormRepository(IPersistenceContext<JsonForm> dbContext, ISecurityProvider<Form> securityProvider = null, MessageBus messageBus = null) : base(dbContext, messageBus)
        {
            this.SecurityProvider = securityProvider;
        }

        public override void AcceptMessage(Updating<JsonForm> update)
        {
            if (update is null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            Type ConcreteForm = GetConcreteFormType(update.Target.Name);

            if (ConcreteForm != null)
            {
                throw new Exception(FORM_NAME_COLLISION_MESSAGE);
            }

            base.AcceptMessage(update);
        }

        public override JsonForm Find(int Id)
        {
            return this.SecurityProvider.TryFind(base.Find(Id));
        }

        public Form GetByName(string Name)
        {
            if (Name is null)
            {
                throw new ArgumentNullException(nameof(Name));
            }

            Type ConcreteForm = GetConcreteFormType(Name);

            if (ConcreteForm is null)
            {
                return this.Where(j => j.ExternalId == Name).ToList().Where(f => this.SecurityProvider.TryCheckAccess(f)).SingleOrDefault();
            }
            else
            {
                if (Activator.CreateInstance(ConcreteForm) is Form toReturn)
                {
                    if (this.SecurityProvider.TryCheckAccess(toReturn))
                    {
                        return toReturn;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    throw new Exception("What the fuck?");
                }
            }
        }

        private static Type GetConcreteFormType(string Name)
        {
            if (Name is null)
            {
                throw new ArgumentNullException(nameof(Name));
            }

            string ExternalId = Name.Replace("-", " ");
            return TypeFactory.GetDerivedTypes(typeof(Form)).Where(t => (t.GetCustomAttribute<DisplayAttribute>()?.Name ?? t.Name) == ExternalId).FirstOrDefault();
        }
    }
}