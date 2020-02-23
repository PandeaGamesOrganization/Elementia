﻿using System.Collections;
using System.Collections.Generic;
using Terra.SerializedData.Entities;

namespace Terra.SerializedData.World
{
    public partial class TerraWorldChunk : IEnumerable<TerraEntity>
    {
        public HashSet<TerraEntity> Entities { get; set; } = new HashSet<TerraEntity>();
        
        public IEnumerator<TerraEntity> GetEnumerator()
        {
            return Entities.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}