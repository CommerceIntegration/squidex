﻿// ==========================================================================
//  ContentDomainObject.cs
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex Group
//  All rights reserved.
// ==========================================================================

using System;
using Squidex.Domain.Apps.Core.Contents;
using Squidex.Domain.Apps.Events.Contents;
using Squidex.Domain.Apps.Write.Contents.Commands;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Commands;
using Squidex.Infrastructure.Dispatching;
using Squidex.Infrastructure.EventSourcing;
using Squidex.Infrastructure.Reflection;

namespace Squidex.Domain.Apps.Write.Contents
{
    public class ContentDomainObject : DomainObjectBase
    {
        private bool isDeleted;
        private bool isCreated;
        private Status status;
        private NamedContentData data;

        public bool IsDeleted
        {
            get { return isDeleted; }
        }

        public Status Status
        {
            get { return status; }
        }

        public NamedContentData Data
        {
            get { return data; }
        }

        public ContentDomainObject(Guid id, int version)
            : base(id, version)
        {
        }

        protected void On(ContentCreated @event)
        {
            isCreated = true;

            data = @event.Data;
        }

        protected void On(ContentUpdated @event)
        {
            data = @event.Data;
        }

        protected void On(ContentStatusChanged @event)
        {
            status = @event.Status;
        }

        protected void On(ContentDeleted @event)
        {
            isDeleted = true;
        }

        public ContentDomainObject Create(CreateContent command)
        {
            VerifyNotCreated();

            RaiseEvent(SimpleMapper.Map(command, new ContentCreated()));

            if (command.Publish)
            {
                RaiseEvent(SimpleMapper.Map(command, new ContentStatusChanged { Status = Status.Published }));
            }

            return this;
        }

        public ContentDomainObject Delete(DeleteContent command)
        {
            VerifyCreatedAndNotDeleted();

            RaiseEvent(SimpleMapper.Map(command, new ContentDeleted()));

            return this;
        }

        public ContentDomainObject ChangeStatus(ChangeContentStatus command)
        {
            VerifyCreatedAndNotDeleted();

            RaiseEvent(SimpleMapper.Map(command, new ContentStatusChanged()));

            return this;
        }

        public ContentDomainObject Update(UpdateContent command)
        {
            VerifyCreatedAndNotDeleted();

            if (!command.Data.Equals(Data))
            {
                RaiseEvent(SimpleMapper.Map(command, new ContentUpdated()));
            }

            return this;
        }

        public ContentDomainObject Patch(PatchContent command)
        {
            VerifyCreatedAndNotDeleted();

            var newData = command.Data.MergeInto(Data);

            if (!newData.Equals(Data))
            {
                RaiseEvent(SimpleMapper.Map(command, new ContentUpdated { Data = newData }));
            }

            return this;
        }

        private void VerifyNotCreated()
        {
            if (isCreated)
            {
                throw new DomainException("Content has already been created.");
            }
        }

        private void VerifyCreatedAndNotDeleted()
        {
            if (isDeleted || !isCreated)
            {
                throw new DomainException("Content has already been deleted or not created yet.");
            }
        }

        protected override void DispatchEvent(Envelope<IEvent> @event)
        {
            this.DispatchAction(@event.Payload);
        }
    }
}
