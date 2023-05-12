using System;
using System.Collections.Generic;

namespace Adriva.Common.Core {
	public sealed class GenericEqualityComparer<T> : EqualityComparer<T> where T : class {

		private Func<T, T, bool> predicate;
		private Func<T, int> action;

		public GenericEqualityComparer(Func<T, T, bool> equalityPredicate, Func<T, int> hashAction) {
			this.predicate = equalityPredicate;
			this.action = hashAction;
		}

		public override bool Equals(T x, T y) {
			if (null == x && null == y) return true;
			if (null == x || null == y) return false;

			return this.predicate(x, y);
		}

		public override int GetHashCode(T obj) {
			if (null == obj) return 0;
			return this.action(obj);
		}
	}
}
