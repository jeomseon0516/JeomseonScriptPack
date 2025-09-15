using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Jeomseon.Data
{
    [Serializable]
    public sealed class JsonDataList<T>
    {
        [JsonProperty] internal List<T> DataList { get; set; }

        public IReadOnlyList<T> Data => DataList;
        internal JsonDataList() {}
    }
}
