﻿namespace CSharpGL
{
    internal abstract class OneIndexPointSearcher
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="x">mouse position(Left Down is (0, 0)).</param>
        /// <param name="y">mouse position(Left Down is (0, 0)).</param>
        /// <param name="primitiveInfo"></param>
        /// <param name="modernRenderer"></param>
        /// <returns></returns>
        internal abstract uint Search(PickEventArgs arg,
            RecognizedPrimitiveInfo primitiveInfo,
            OneIndexPicker picker);
    }
}