﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.ValueGeneration.Internal
{
    public class NpgsqlValueGeneratorSelector : RelationalValueGeneratorSelector
    {
        readonly INpgsqlSequenceValueGeneratorFactory _sequenceFactory;

        readonly NpgsqlRelationalConnection _connection;

        public NpgsqlValueGeneratorSelector(
            [NotNull] IValueGeneratorCache cache,
            [NotNull] INpgsqlSequenceValueGeneratorFactory sequenceFactory,
            [NotNull] NpgsqlRelationalConnection connection,
            [NotNull] IRelationalAnnotationProvider relationalExtensions)
            : base(cache, relationalExtensions)
        {
            _sequenceFactory = sequenceFactory;
            _connection = connection;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public new virtual INpgsqlValueGeneratorCache Cache => (INpgsqlValueGeneratorCache)base.Cache;

        public override ValueGenerator Select(IProperty property, IEntityType entityType)
        {
            Check.NotNull(property, nameof(property));
            Check.NotNull(entityType, nameof(entityType));

            return property.GetValueGeneratorFactory() == null
                   && property.Npgsql().ValueGenerationStrategy == NpgsqlValueGenerationStrategy.SequenceHiLo
                ? _sequenceFactory.Create(property, Cache.GetOrAddSequenceState(property), _connection)
                : base.Select(property, entityType);
        }

        public override ValueGenerator Create(IProperty property, IEntityType entityType)
        {
            Check.NotNull(property, nameof(property));
            Check.NotNull(entityType, nameof(entityType));

            // Generate temporary values if the user specified a default value (to allow
            // generating server-side with uuid-ossp or whatever)
            return property.ClrType.UnwrapNullableType() == typeof(Guid)
                ? property.ValueGenerated == ValueGenerated.Never
                  || property.Npgsql().DefaultValueSql != null
                    ? new TemporaryGuidValueGenerator()
                    : new GuidValueGenerator()
                : base.Create(property, entityType);
        }
    }
}
