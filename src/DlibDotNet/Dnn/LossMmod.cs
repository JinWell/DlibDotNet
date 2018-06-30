﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DlibDotNet.Extensions;

namespace DlibDotNet.Dnn
{

    public sealed class LossMmod : Net
    {

        #region Constructors

        public LossMmod(int type = 0)
            : base(type)
        {
            this.NativePtr = Native.loss_mmod_new(type);
        }

        internal LossMmod(IntPtr ptr, int type = 0)
            : base(type)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentException("Can not pass IntPtr.Zero", nameof(ptr));

            this.NativePtr = ptr;
        }

        #endregion

        #region Methods

        public static LossMmod Deserialize(string path, int type = 0)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path))
                throw new FileNotFoundException($"{path} is not found", path);

            var str = Encoding.UTF8.GetBytes(path);
            var ret = Native.loss_mmod_deserialize(str, type);
            return new LossMmod(ret, type);
        }

        public Output Operator<T>(Matrix<T> image)
            where T : struct
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            image.ThrowIfDisposed();

            var ret = Native.loss_mmod_operator_matrix(this.NativePtr,
                                                       this.Type,
                                                       image.MatrixElementType.ToNativeMatrixElementType(),
                                                       image.NativePtr,
                                                       out var output);
            switch (ret)
            {
                case Dlib.Native.ErrorType.MatrixElementTypeNotSupport:
                    throw new ArgumentException($"{image.MatrixElementType} is not supported.");
            }

            return new Output(output);
        }

        public Outputs Operator<T>(IEnumerable<Matrix<T>> images)
            where T : struct
        {
            if (images == null)
                throw new ArgumentNullException(nameof(images));
            if (!images.Any())
                throw new ArgumentException();
            if (images.Any(matrix => matrix == null))
                throw new ArgumentException();

            images.ThrowIfDisposed();

            using (var vecIn = new StdVector<Matrix<T>>(images))
            {
                Matrix<T>.TryParse<T>(out var imageType);
                var templateRows = images.First().TemplateRows;
                var templateColumns = images.First().TemplateColumns;

                // vecOut is not std::vector<std::vector<mmod_rect>*>* but std::vector<std::vector<mmod_rect>>*.
                var ret = Native.loss_mmod_operator_matrixs(this.NativePtr,
                                                            this.Type,
                                                            imageType.ToNativeMatrixElementType(),
                                                            vecIn.NativePtr,
                                                            templateRows,
                                                            templateColumns,
                                                            out var vecOut);

                switch (ret)
                {
                    case Dlib.Native.ErrorType.MatrixElementTypeNotSupport:
                        throw new ArgumentException($"{imageType} is not supported.");
                }

                return new Outputs(vecOut);
            }
        }

        #region Overrides 

        protected override void DisposeUnmanaged()
        {
            base.DisposeUnmanaged();
            Native.loss_mmod_delete(this.NativePtr, this.Type);
        }

        #endregion

        #endregion

        public sealed class Output : DlibObject, IEnumerable<MModRect>
        {

            #region Fields

            private readonly int _Size;

            #endregion

            #region Constructors

            internal Output(IntPtr output)
            {
                this.NativePtr = output;
                this._Size = Native.dnn_output_stdvector_mmod_rect_getSize(output);
            }

            #endregion

            #region Properties

            public int Length
            {
                get
                {
                    this.ThrowIfDisposed();
                    return this._Size;
                }
            }

            public MModRect this[int index]
            {
                get
                {
                    this.ThrowIfDisposed();

                    if (!(0 <= index && index < this._Size))
                        throw new ArgumentOutOfRangeException();

                    var ptr = Native.dnn_output_stdvector_mmod_rect_getItem(this.NativePtr, index);
                    return new MModRect(ptr, false);
                }
            }

            public MModRect this[uint index]
            {
                get
                {
                    this.ThrowIfDisposed();

                    if (!(index < this._Size))
                        throw new ArgumentOutOfRangeException();

                    var ptr = Native.dnn_output_stdvector_mmod_rect_getItem(this.NativePtr, (int)index);
                    return new MModRect(ptr, false);
                }
            }

            #endregion

            #region Methods

            #region Overrides

            protected override void DisposeUnmanaged()
            {
                base.DisposeUnmanaged();
                Native.dnn_output_stdvector_mmod_rect_delete(this.NativePtr);
            }

            #endregion

            #endregion

            #region IEnumerable<TItem> Members

            public IEnumerator<MModRect> GetEnumerator()
            {
                this.ThrowIfDisposed();

                for (var index = 0; index < this._Size; index++)
                {
                    var ptr = Native.dnn_output_stdvector_mmod_rect_getItem(this.NativePtr, index);
                    yield return new MModRect(ptr, false);
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion

            internal sealed class Native
            {

                [DllImport(NativeMethods.NativeDnnLibrary, CallingConvention = NativeMethods.CallingConvention)]
                public static extern void dnn_output_stdvector_mmod_rect_delete(IntPtr vector);

                [DllImport(NativeMethods.NativeDnnLibrary, CallingConvention = NativeMethods.CallingConvention)]
                public static extern IntPtr dnn_output_stdvector_mmod_rect_getItem(IntPtr vector, int index);

                [DllImport(NativeMethods.NativeDnnLibrary, CallingConvention = NativeMethods.CallingConvention)]
                public static extern int dnn_output_stdvector_mmod_rect_getSize(IntPtr vector);

            }

        }

        public sealed class Outputs : DlibObject, IUndisposableElementCollection<IEnumerable<MModRect>>
        {

            #region Fields

            private readonly int _Size;

            #endregion

            #region Constructors

            internal Outputs(IntPtr output)
            {
                this.NativePtr = output;
                this._Size = Native.dnn_output_stdvector_stdvector_mmod_rect_getSize(output);
            }

            #endregion

            #region Properties

            public int Length
            {
                get
                {
                    this.ThrowIfDisposed();
                    return this._Size;
                }
            }

            public Matrix<float> this[int index]
            {
                get
                {
                    this.ThrowIfDisposed();

                    if (!(0 <= index && index < this._Size))
                        throw new ArgumentOutOfRangeException();

                    var ptr = Native.dnn_output_stdvector_stdvector_mmod_rect_getItem(this.NativePtr, index);
                    return new Matrix<float>(ptr, 0, 1, false);
                }
            }

            public Matrix<float> this[uint index]
            {
                get
                {
                    this.ThrowIfDisposed();

                    if (!(index < this._Size))
                        throw new ArgumentOutOfRangeException();

                    var ptr = Native.dnn_output_stdvector_stdvector_mmod_rect_getItem(this.NativePtr, (int)index);
                    return new Matrix<float>(ptr, 0, 1, false);
                }
            }

            #endregion

            #region Methods

            #region Overrides

            protected override void DisposeUnmanaged()
            {
                base.DisposeUnmanaged();
                Native.dnn_output_stdvector_stdvector_mmod_rect_delete(this.NativePtr);
            }

            #endregion

            #endregion

            #region IEnumerable<TItem> Members

            public IEnumerator<IEnumerable<MModRect>> GetEnumerator()
            {
                this.ThrowIfDisposed();

                for (var index = 0; index < this._Size; index++)
                {
                    var ptr = Native.dnn_output_stdvector_stdvector_mmod_rect_getItem(this.NativePtr, index);
                    using (var vector = new StdVector<MModRect>())
                        yield return vector.ToArray();
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                this.ThrowIfDisposed();

                return this.GetEnumerator();
            }

            #endregion

            internal sealed class Native
            {

                [DllImport(NativeMethods.NativeDnnLibrary, CallingConvention = NativeMethods.CallingConvention)]
                public static extern void dnn_output_stdvector_stdvector_mmod_rect_delete(IntPtr vector);

                [DllImport(NativeMethods.NativeDnnLibrary, CallingConvention = NativeMethods.CallingConvention)]
                public static extern IntPtr dnn_output_stdvector_stdvector_mmod_rect_getItem(IntPtr vector, int index);

                [DllImport(NativeMethods.NativeDnnLibrary, CallingConvention = NativeMethods.CallingConvention)]
                public static extern int dnn_output_stdvector_stdvector_mmod_rect_getSize(IntPtr vector);

            }

        }

        internal sealed class Native
        {

            [DllImport(NativeMethods.NativeDnnLibrary, CallingConvention = NativeMethods.CallingConvention)]
            public static extern IntPtr loss_mmod_new(int type);

            [DllImport(NativeMethods.NativeDnnLibrary, CallingConvention = NativeMethods.CallingConvention)]
            public static extern void loss_mmod_delete(IntPtr obj, int type);

            [DllImport(NativeMethods.NativeDnnLibrary, CallingConvention = NativeMethods.CallingConvention)]
            public static extern IntPtr loss_mmod_deserialize(byte[] fileName, int type);

            [DllImport(NativeMethods.NativeDnnLibrary, CallingConvention = NativeMethods.CallingConvention)]
            public static extern Dlib.Native.ErrorType loss_mmod_operator_matrix(IntPtr obj, int type, Dlib.Native.MatrixElementType element_type, IntPtr matrix, out IntPtr ret);

            [DllImport(NativeMethods.NativeDnnLibrary, CallingConvention = NativeMethods.CallingConvention)]
            public static extern Dlib.Native.ErrorType loss_mmod_operator_matrixs(IntPtr obj, int type, Dlib.Native.MatrixElementType element_type, IntPtr matrixs, int templateRows, int templateColumns, out IntPtr ret);

        }

    }

}