﻿//***************************************************************************************************
//* (C) ColorfulSoft corp., 2019-2023. All rights reserved.
//* The code is available under the Apache-2.0 license. Read the License for details.
//***************************************************************************************************

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.AI.Experimental
{

    public unsafe static partial class torchlite
    {

        /// <summary>
        /// Adds other, scaled by alpha, to input.
        /// </summary>
        /// <param name="input">The input tensor.</param>
        /// <param name="other">The tensor or number to add to input.</param>
        /// <param name="alpha">The multiplier for other.</param>
        /// <returns>Tensor.</returns>
        public static Tensor add(this Tensor input,
                                 Tensor other,
                                 object alpha = null)
        {
            if((input == null) || (other == null))
            {
                throw new NullReferenceException("Null value is invalid for operand.");
            }
            alpha = alpha ?? 1;
            // Get unmanaged shapes and compute strides
            var left_ndim = input.shape.ndim;
            var left_shape = input.shape.data_ptr;
            var left_strides = (int*)Marshal.AllocHGlobal(left_ndim * sizeof(int));
            if(left_ndim > 0)
            {
                left_strides[left_ndim - 1] = 1;
            }
            for(int i = left_ndim - 2; i >= 0; --i)
            {
                left_strides[i] = left_strides[i + 1] * left_shape[i + 1];
            }
            var right_ndim = other.shape.ndim;
            var right_shape = other.shape.data_ptr;
            var right_strides = (int*)Marshal.AllocHGlobal(right_ndim * sizeof(int));
            if(right_ndim > 0)
            {
                right_strides[right_ndim - 1] = 1;
            }
            for(int i = right_ndim - 2; i >= 0; --i)
            {
                right_strides[i] = right_strides[i + 1] * right_shape[i + 1];
            }
            // Compute result shape and broadcasting flags
            var result_ndim = Math.Max(left_ndim, right_ndim);
            var result_shape_ = new int[result_ndim];
            var left_broadcast_dims = (int*)Marshal.AllocHGlobal(left_ndim * sizeof(int));
            var right_broadcast_dims = (int*)Marshal.AllocHGlobal(right_ndim * sizeof(int));
            for(int i = 0; i < result_ndim; ++i)
            {
                int az = (((left_ndim - i - 1) < left_ndim) && ((left_ndim - i - 1) > -1)) ? left_shape[left_ndim - i - 1] : 1;
                int bz = (((right_ndim - i - 1) < right_ndim) && ((right_ndim - i - 1) > -1)) ? right_shape[right_ndim - i - 1] : 1;
                if((az > 1) && (bz > 1) && (az != bz))
                {
                    throw new ArgumentException("Tensors are not broadcastable.");
                }
                result_shape_[result_ndim - 1 - i] = Math.Max(az, bz);
                if(i < left_ndim)
                {
                    var dimA = left_ndim - 1 - i;
                    var azA = ((dimA > -1) && (dimA < left_ndim)) ? left_shape[dimA] : 1;
                    var bzA = (((result_ndim - i - 1) > -1) && ((result_ndim - 1 - i) < result_ndim)) ? result_shape_[result_ndim - 1 - i] : 1;
                    if((bzA > 1) && (azA == 1))
                    {
                        left_broadcast_dims[(left_ndim - 1) - i] = 1;
                    }
                    else
                    {
                        left_broadcast_dims[(left_ndim - 1) - i] = 0;
                    }
                }
                if(i < right_ndim)
                {
                    var dimB = right_ndim - 1 - i;
                    var azB = ((dimB > -1) && (dimB < right_ndim)) ? right_shape[dimB] : 1;
                    var bzB = (((result_ndim - i - 1) > -1) && ((result_ndim - 1 - i) < result_ndim)) ? result_shape_[result_ndim - 1 - i] : 1;
                    if((bzB > 1) && (azB == 1))
                    {
                        right_broadcast_dims[(right_ndim - 1) - i] = 1;
                    }
                    else
                    {
                        right_broadcast_dims[(right_ndim - 1) - i] = 0;
                    }
                }
            }
            var result_strides = (int*)Marshal.AllocHGlobal(result_ndim * sizeof(int));
            var result_numel = 1;
            if(result_ndim > 0)
            {
                result_strides[result_ndim - 1] = 1;
                result_numel *= result_shape_[result_ndim - 1];
            }
            for(int i = result_ndim - 2; i >= 0; --i)
            {
                result_strides[i] = result_strides[i + 1] * result_shape_[i + 1];
                result_numel *= result_shape_[i];
            }
            // Get parents for result tensor
            var parents = new List<Tensor>();
            if(input.requires_grad)
            {
                parents.Add(input);
            }
            if(other.requires_grad)
            {
                parents.Add(other);
            }
            // Compute!
            switch(input.dtype)
            {
                case torchlite.float32:
                {
                    var left_ptr = (float*)input.storage.data_ptr;
                    switch(other.dtype)
                    {
                        case torchlite.float32:
                        {
                            if(!((alpha is float) || (alpha is double) ||
                                 (alpha is byte) || (alpha is sbyte) ||
                                 (alpha is short) || (alpha is ushort) ||
                                 (alpha is int) || (alpha is uint) ||
                                 (alpha is long) || (alpha is ulong) ||
                                 (alpha is bool)))
                            {
                                throw new ArgumentException("For floating point input tensors, argument alpha should be of floating point, integer or bool data type.");
                            }
                            float alpha_ = Convert.ToSingle(alpha);
                            var result = new Tensor(result_shape_, torchlite.float32, input.requires_grad || other.requires_grad);
                            var result_shape = result.shape.data_ptr;
                            var right_ptr = (float*)other.storage.data_ptr;
                            var result_ptr = (float*)result.storage.data_ptr;
                            for(int i = 0; i < result_numel; ++i)
                            {
                                var index = i;
                                int li = 0;
                                int ldim = 0;
                                int ri = 0;
                                int rdim = 0;
                                for(int j = 0; j < result_ndim; ++j)
                                {
                                    int loc = index / result_strides[j];
                                    index -= loc * result_strides[j];
                                    if(j >= (result_ndim - left_ndim))
                                    {
                                        int temp = loc;
                                        if(left_broadcast_dims[ldim] > 0)
                                        {
                                            temp = 0;
                                        }
                                        li += left_strides[ldim++] * temp;
                                    }
                                    if(j >= (result_ndim - right_ndim))
                                    {
                                        int temp = loc;
                                        if(right_broadcast_dims[rdim] > 0)
                                        {
                                            temp = 0;
                                        }
                                        ri += right_strides[rdim++] * temp;
                                    }
                                }
                                result_ptr[i] = left_ptr[li] + right_ptr[ri] * alpha_;
                            }
                            if(result.requires_grad)
                            {
                                result.__parents = parents.ToArray();
                                result.backward_fn = () =>
                                {
                                    var result_grad_ptr = (float*)result.grad.storage.data_ptr;
                                    float* left_grad_ptr;
                                    float* right_grad_ptr;
                                    if(!other.requires_grad)
                                    {
                                        left_grad_ptr = (float*)input.grad.storage.data_ptr;
                                        for(int i = 0; i < result_numel; ++i)
                                        {
                                            var index = i;
                                            int li = 0;
                                            int ldim = 0;
                                            for(int j = 0; j < result_ndim; ++j)
                                            {
                                                int loc = index / result_strides[j];
                                                index -= loc * result_strides[j];
                                                if(j >= (result_ndim - left_ndim))
                                                {
                                                    int temp = loc;
                                                    if(left_broadcast_dims[ldim] > 0)
                                                    {
                                                        temp = 0;
                                                    }
                                                    li += left_strides[ldim++] * temp;
                                                }
                                            }
                                            left_grad_ptr[li] += result_grad_ptr[i];
                                        }
                                        Marshal.FreeHGlobal((IntPtr)left_strides);
                                        Marshal.FreeHGlobal((IntPtr)right_strides);
                                        Marshal.FreeHGlobal((IntPtr)result_strides);
                                        Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                                        Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                                        return;
                                    }
                                    if(!input.requires_grad)
                                    {
                                        right_grad_ptr = (float*)other.grad.storage.data_ptr;
                                        for(int i = 0; i < result_numel; ++i)
                                        {
                                            var index = i;
                                            int ri = 0;
                                            int rdim = 0;
                                            for(int j = 0; j < result_ndim; ++j)
                                            {
                                                int loc = index / result_strides[j];
                                                index -= loc * result_strides[j];
                                                if(j >= (result_ndim - right_ndim))
                                                {
                                                    int temp = loc;
                                                    if(right_broadcast_dims[rdim] > 0)
                                                    {
                                                        temp = 0;
                                                    }
                                                    ri += right_strides[rdim++] * temp;
                                                }
                                            }
                                            right_grad_ptr[ri] += result_grad_ptr[i] * alpha_;
                                        }
                                        Marshal.FreeHGlobal((IntPtr)left_strides);
                                        Marshal.FreeHGlobal((IntPtr)right_strides);
                                        Marshal.FreeHGlobal((IntPtr)result_strides);
                                        Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                                        Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                                        return;
                                    }
                                    left_grad_ptr = (float*)input.grad.storage.data_ptr;
                                    right_grad_ptr = (float*)other.grad.storage.data_ptr;
                                    for(int i = 0; i < result_numel; ++i)
                                    {
                                        var index = i;
                                        int li = 0;
                                        int ldim = 0;
                                        int ri = 0;
                                        int rdim = 0;
                                        for(int j = 0; j < result_ndim; ++j)
                                        {
                                            int loc = index / result_strides[j];
                                            index -= loc * result_strides[j];
                                            if(j >= (result_ndim - left_ndim))
                                            {
                                                int temp = loc;
                                                if(left_broadcast_dims[ldim] > 0)
                                                {
                                                    temp = 0;
                                                }
                                                li += left_strides[ldim++] * temp;
                                            }
                                            if(j >= (result_ndim - right_ndim))
                                            {
                                                int temp = loc;
                                                if(right_broadcast_dims[rdim] > 0)
                                                {
                                                    temp = 0;
                                                }
                                                ri += right_strides[rdim++] * temp;
                                            }
                                        }
                                        left_grad_ptr[li] += result_grad_ptr[i];
                                        right_grad_ptr[ri] += result_grad_ptr[i] * alpha_;
                                    }
                                    Marshal.FreeHGlobal((IntPtr)left_strides);
                                    Marshal.FreeHGlobal((IntPtr)right_strides);
                                    Marshal.FreeHGlobal((IntPtr)result_strides);
                                    Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                                    Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                                };
                            }
                            else
                            {
                                Marshal.FreeHGlobal((IntPtr)left_strides);
                                Marshal.FreeHGlobal((IntPtr)right_strides);
                                Marshal.FreeHGlobal((IntPtr)result_strides);
                                Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                                Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                            }
                            return result;
                        }
                        case torchlite.int32:
                        {
                            if(!((alpha is float) || (alpha is double) ||
                                 (alpha is byte) || (alpha is sbyte) ||
                                 (alpha is short) || (alpha is ushort) ||
                                 (alpha is int) || (alpha is uint) ||
                                 (alpha is long) || (alpha is ulong) ||
                                 (alpha is bool)))
                            {
                                throw new ArgumentException("For floating point input tensors, argument alpha should be of floating point, integer or bool data type.");
                            }
                            float alpha_ = Convert.ToSingle(alpha);
                            var result = new Tensor(result_shape_, torchlite.float32, input.requires_grad);
                            var result_shape = result.shape.data_ptr;
                            var right_ptr = (int*)other.storage.data_ptr;
                            var result_ptr = (float*)result.storage.data_ptr;
                            for(int i = 0; i < result_numel; ++i)
                            {
                                var index = i;
                                int li = 0;
                                int ldim = 0;
                                int ri = 0;
                                int rdim = 0;
                                for(int j = 0; j < result_ndim; ++j)
                                {
                                    int loc = index / result_strides[j];
                                    index -= loc * result_strides[j];
                                    if(j >= (result_ndim - left_ndim))
                                    {
                                        int temp = loc;
                                        if(left_broadcast_dims[ldim] > 0)
                                        {
                                            temp = 0;
                                        }
                                        li += left_strides[ldim++] * temp;
                                    }
                                    if(j >= (result_ndim - right_ndim))
                                    {
                                        int temp = loc;
                                        if(right_broadcast_dims[rdim] > 0)
                                        {
                                            temp = 0;
                                        }
                                        ri += right_strides[rdim++] * temp;
                                    }
                                }
                                result_ptr[i] = left_ptr[li] + right_ptr[ri] * alpha_;
                            }
                            if(result.requires_grad)
                            {
                                result.__parents = parents.ToArray();
                                result.backward_fn = () =>
                                {
                                    var result_grad_ptr = (float*)result.grad.storage.data_ptr;
                                    var left_grad_ptr = (float*)input.grad.storage.data_ptr;
                                    for(int i = 0; i < result_numel; ++i)
                                    {
                                        var index = i;
                                        int li = 0;
                                        int ldim = 0;
                                        for(int j = 0; j < result_ndim; ++j)
                                        {
                                            int loc = index / result_strides[j];
                                            index -= loc * result_strides[j];
                                            if(j >= (result_ndim - left_ndim))
                                            {
                                                int temp = loc;
                                                if(left_broadcast_dims[ldim] > 0)
                                                {
                                                    temp = 0;
                                                }
                                                li += left_strides[ldim++] * temp;
                                            }
                                        }
                                        left_grad_ptr[li] += result_grad_ptr[i];
                                    }
                                    Marshal.FreeHGlobal((IntPtr)left_strides);
                                    Marshal.FreeHGlobal((IntPtr)right_strides);
                                    Marshal.FreeHGlobal((IntPtr)result_strides);
                                    Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                                    Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                                };
                            }
                            else
                            {
                                Marshal.FreeHGlobal((IntPtr)left_strides);
                                Marshal.FreeHGlobal((IntPtr)right_strides);
                                Marshal.FreeHGlobal((IntPtr)result_strides);
                                Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                                Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                            }
                            return result;
                        }
                        case torchlite.@bool:
                        {
                            if(!((alpha is float) || (alpha is double) ||
                                 (alpha is byte) || (alpha is sbyte) ||
                                 (alpha is short) || (alpha is ushort) ||
                                 (alpha is int) || (alpha is uint) ||
                                 (alpha is long) || (alpha is ulong) ||
                                 (alpha is bool)))
                            {
                                throw new ArgumentException("For floating point input tensors, argument alpha should be of floating point, integer or bool data type.");
                            }
                            float alpha_ = Convert.ToSingle(alpha);
                            var result = new Tensor(result_shape_, torchlite.float32, input.requires_grad);
                            var result_shape = result.shape.data_ptr;
                            var right_ptr = (byte*)other.storage.data_ptr;
                            var result_ptr = (float*)result.storage.data_ptr;
                            for(int i = 0; i < result_numel; ++i)
                            {
                                var index = i;
                                int li = 0;
                                int ldim = 0;
                                int ri = 0;
                                int rdim = 0;
                                for(int j = 0; j < result_ndim; ++j)
                                {
                                    int loc = index / result_strides[j];
                                    index -= loc * result_strides[j];
                                    if(j >= (result_ndim - left_ndim))
                                    {
                                        int temp = loc;
                                        if(left_broadcast_dims[ldim] > 0)
                                        {
                                            temp = 0;
                                        }
                                        li += left_strides[ldim++] * temp;
                                    }
                                    if(j >= (result_ndim - right_ndim))
                                    {
                                        int temp = loc;
                                        if(right_broadcast_dims[rdim] > 0)
                                        {
                                            temp = 0;
                                        }
                                        ri += right_strides[rdim++] * temp;
                                    }
                                }
                                result_ptr[i] = left_ptr[li] + right_ptr[ri] * alpha_;
                            }
                            if(result.requires_grad)
                            {
                                result.__parents = parents.ToArray();
                                result.backward_fn = () =>
                                {
                                    var result_grad_ptr = (float*)result.grad.storage.data_ptr;
                                    var left_grad_ptr = (float*)input.grad.storage.data_ptr;
                                    for(int i = 0; i < result_numel; ++i)
                                    {
                                        var index = i;
                                        int li = 0;
                                        int ldim = 0;
                                        for(int j = 0; j < result_ndim; ++j)
                                        {
                                            int loc = index / result_strides[j];
                                            index -= loc * result_strides[j];
                                            if(j >= (result_ndim - left_ndim))
                                            {
                                                int temp = loc;
                                                if(left_broadcast_dims[ldim] > 0)
                                                {
                                                    temp = 0;
                                                }
                                                li += left_strides[ldim++] * temp;
                                            }
                                        }
                                        left_grad_ptr[li] += result_grad_ptr[i];
                                    }
                                    Marshal.FreeHGlobal((IntPtr)left_strides);
                                    Marshal.FreeHGlobal((IntPtr)right_strides);
                                    Marshal.FreeHGlobal((IntPtr)result_strides);
                                    Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                                    Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                                };
                            }
                            else
                            {
                                Marshal.FreeHGlobal((IntPtr)left_strides);
                                Marshal.FreeHGlobal((IntPtr)right_strides);
                                Marshal.FreeHGlobal((IntPtr)result_strides);
                                Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                                Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                            }
                            return result;
                        }
                        default:
                        {
                            Marshal.FreeHGlobal((IntPtr)left_strides);
                            Marshal.FreeHGlobal((IntPtr)right_strides);
                            Marshal.FreeHGlobal((IntPtr)result_strides);
                            Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                            Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                            throw new NotImplementedException();
                        }
                    }
                }
                case torchlite.int32:
                {
                    var left_ptr = (int*)input.storage.data_ptr;
                    switch(other.dtype)
                    {
                        case torchlite.float32:
                        {
                            if(!((alpha is float) || (alpha is double) ||
                                 (alpha is byte) || (alpha is sbyte) ||
                                 (alpha is short) || (alpha is ushort) ||
                                 (alpha is int) || (alpha is uint) ||
                                 (alpha is long) || (alpha is ulong) ||
                                 (alpha is bool)))
                            {
                                throw new ArgumentException("For floating point input tensors, argument alpha should be of floating point, integer or bool data type.");
                            }
                            float alpha_ = Convert.ToSingle(alpha);
                            var result = new Tensor(result_shape_, torchlite.float32, other.requires_grad);
                            var result_shape = result.shape.data_ptr;
                            var right_ptr = (float*)other.storage.data_ptr;
                            var result_ptr = (float*)result.storage.data_ptr;
                            for(int i = 0; i < result_numel; ++i)
                            {
                                var index = i;
                                int li = 0;
                                int ldim = 0;
                                int ri = 0;
                                int rdim = 0;
                                for(int j = 0; j < result_ndim; ++j)
                                {
                                    int loc = index / result_strides[j];
                                    index -= loc * result_strides[j];
                                    if(j >= (result_ndim - left_ndim))
                                    {
                                        int temp = loc;
                                        if(left_broadcast_dims[ldim] > 0)
                                        {
                                            temp = 0;
                                        }
                                        li += left_strides[ldim++] * temp;
                                    }
                                    if(j >= (result_ndim - right_ndim))
                                    {
                                        int temp = loc;
                                        if(right_broadcast_dims[rdim] > 0)
                                        {
                                            temp = 0;
                                        }
                                        ri += right_strides[rdim++] * temp;
                                    }
                                }
                                result_ptr[i] = left_ptr[li] + right_ptr[ri] * alpha_;
                            }
                            if(result.requires_grad)
                            {
                                result.__parents = parents.ToArray();
                                result.backward_fn = () =>
                                {
                                    var result_grad_ptr = (float*)result.grad.storage.data_ptr;
                                    float* right_grad_ptr;
                                    right_grad_ptr = (float*)other.grad.storage.data_ptr;
                                    for(int i = 0; i < result_numel; ++i)
                                    {
                                        var index = i;
                                        int ri = 0;
                                        int rdim = 0;
                                        for(int j = 0; j < result_ndim; ++j)
                                        {
                                            int loc = index / result_strides[j];
                                            index -= loc * result_strides[j];
                                            if(j >= (result_ndim - right_ndim))
                                            {
                                                int temp = loc;
                                                if(right_broadcast_dims[rdim] > 0)
                                                {
                                                    temp = 0;
                                                }
                                                ri += right_strides[rdim++] * temp;
                                            }
                                        }
                                        right_grad_ptr[ri] += result_grad_ptr[i] * alpha_;
                                    }
                                    Marshal.FreeHGlobal((IntPtr)left_strides);
                                    Marshal.FreeHGlobal((IntPtr)right_strides);
                                    Marshal.FreeHGlobal((IntPtr)result_strides);
                                    Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                                    Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                                };
                            }
                            else
                            {
                                Marshal.FreeHGlobal((IntPtr)left_strides);
                                Marshal.FreeHGlobal((IntPtr)right_strides);
                                Marshal.FreeHGlobal((IntPtr)result_strides);
                                Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                                Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                            }
                            return result;
                        }
                        case torchlite.int32:
                        {
                            if(!((alpha is byte) || (alpha is sbyte) ||
                                 (alpha is short) || (alpha is ushort) ||
                                 (alpha is int) || (alpha is uint) ||
                                 (alpha is long) || (alpha is ulong) ||
                                 (alpha is bool)))
                            {
                                throw new ArgumentException("For integer input tensors, argument alpha should be of integer or bool data type.");
                            }
                            int alpha_ = Convert.ToInt32(alpha);
                            var result = new Tensor(result_shape_, torchlite.int32);
                            var result_shape = result.shape.data_ptr;
                            var right_ptr = (int*)other.storage.data_ptr;
                            var result_ptr = (int*)result.storage.data_ptr;
                            for(int i = 0; i < result_numel; ++i)
                            {
                                var index = i;
                                int li = 0;
                                int ldim = 0;
                                int ri = 0;
                                int rdim = 0;
                                for(int j = 0; j < result_ndim; ++j)
                                {
                                    int loc = index / result_strides[j];
                                    index -= loc * result_strides[j];
                                    if(j >= (result_ndim - left_ndim))
                                    {
                                        int temp = loc;
                                        if(left_broadcast_dims[ldim] > 0)
                                        {
                                            temp = 0;
                                        }
                                        li += left_strides[ldim++] * temp;
                                    }
                                    if(j >= (result_ndim - right_ndim))
                                    {
                                        int temp = loc;
                                        if(right_broadcast_dims[rdim] > 0)
                                        {
                                            temp = 0;
                                        }
                                        ri += right_strides[rdim++] * temp;
                                    }
                                }
                                result_ptr[i] = left_ptr[li] + right_ptr[ri] * alpha_;
                            }
                            Marshal.FreeHGlobal((IntPtr)left_strides);
                            Marshal.FreeHGlobal((IntPtr)right_strides);
                            Marshal.FreeHGlobal((IntPtr)result_strides);
                            Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                            Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                            return result;
                        }
                        case torchlite.@bool:
                        {
                            if(!((alpha is byte) || (alpha is sbyte) ||
                                 (alpha is short) || (alpha is ushort) ||
                                 (alpha is int) || (alpha is uint) ||
                                 (alpha is long) || (alpha is ulong) ||
                                 (alpha is bool)))
                            {
                                throw new ArgumentException("For integer input tensors, argument alpha should be of integer or bool data type.");
                            }
                            int alpha_ = Convert.ToInt32(alpha);
                            var result = new Tensor(result_shape_, torchlite.int32);
                            var result_shape = result.shape.data_ptr;
                            var right_ptr = (byte*)other.storage.data_ptr;
                            var result_ptr = (int*)result.storage.data_ptr;
                            for(int i = 0; i < result_numel; ++i)
                            {
                                var index = i;
                                int li = 0;
                                int ldim = 0;
                                int ri = 0;
                                int rdim = 0;
                                for(int j = 0; j < result_ndim; ++j)
                                {
                                    int loc = index / result_strides[j];
                                    index -= loc * result_strides[j];
                                    if(j >= (result_ndim - left_ndim))
                                    {
                                        int temp = loc;
                                        if(left_broadcast_dims[ldim] > 0)
                                        {
                                            temp = 0;
                                        }
                                        li += left_strides[ldim++] * temp;
                                    }
                                    if(j >= (result_ndim - right_ndim))
                                    {
                                        int temp = loc;
                                        if(right_broadcast_dims[rdim] > 0)
                                        {
                                            temp = 0;
                                        }
                                        ri += right_strides[rdim++] * temp;
                                    }
                                }
                                result_ptr[i] = left_ptr[li] + right_ptr[ri] * alpha_;
                            }
                            Marshal.FreeHGlobal((IntPtr)left_strides);
                            Marshal.FreeHGlobal((IntPtr)right_strides);
                            Marshal.FreeHGlobal((IntPtr)result_strides);
                            Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                            Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                            return result;
                        }
                        default:
                        {
                            Marshal.FreeHGlobal((IntPtr)left_strides);
                            Marshal.FreeHGlobal((IntPtr)right_strides);
                            Marshal.FreeHGlobal((IntPtr)result_strides);
                            Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                            Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                            throw new NotImplementedException();
                        }
                    }
                }
                case torchlite.@bool:
                {
                    var left_ptr = (byte*)input.storage.data_ptr;
                    switch(other.dtype)
                    {
                        case torchlite.float32:
                        {
                            if(!((alpha is float) || (alpha is double) ||
                                 (alpha is byte) || (alpha is sbyte) ||
                                 (alpha is short) || (alpha is ushort) ||
                                 (alpha is int) || (alpha is uint) ||
                                 (alpha is long) || (alpha is ulong) ||
                                 (alpha is bool)))
                            {
                                throw new ArgumentException("For floating point input tensors, argument alpha should be of float, integer or bool data type.");
                            }
                            float alpha_ = Convert.ToSingle(alpha);
                            var result = new Tensor(result_shape_, torchlite.float32, other.requires_grad);
                            var result_shape = result.shape.data_ptr;
                            var right_ptr = (float*)other.storage.data_ptr;
                            var result_ptr = (float*)result.storage.data_ptr;
                            for(int i = 0; i < result_numel; ++i)
                            {
                                var index = i;
                                int li = 0;
                                int ldim = 0;
                                int ri = 0;
                                int rdim = 0;
                                for(int j = 0; j < result_ndim; ++j)
                                {
                                    int loc = index / result_strides[j];
                                    index -= loc * result_strides[j];
                                    if(j >= (result_ndim - left_ndim))
                                    {
                                        int temp = loc;
                                        if(left_broadcast_dims[ldim] > 0)
                                        {
                                            temp = 0;
                                        }
                                        li += left_strides[ldim++] * temp;
                                    }
                                    if(j >= (result_ndim - right_ndim))
                                    {
                                        int temp = loc;
                                        if(right_broadcast_dims[rdim] > 0)
                                        {
                                            temp = 0;
                                        }
                                        ri += right_strides[rdim++] * temp;
                                    }
                                }
                                result_ptr[i] = left_ptr[li] + right_ptr[ri] * alpha_;
                            }
                            if(result.requires_grad)
                            {
                                result.__parents = parents.ToArray();
                                result.backward_fn = () =>
                                {
                                    var result_grad_ptr = (float*)result.grad.storage.data_ptr;
                                    float* right_grad_ptr;
                                    right_grad_ptr = (float*)other.grad.storage.data_ptr;
                                    for(int i = 0; i < result_numel; ++i)
                                    {
                                        var index = i;
                                        int ri = 0;
                                        int rdim = 0;
                                        for(int j = 0; j < result_ndim; ++j)
                                        {
                                            int loc = index / result_strides[j];
                                            index -= loc * result_strides[j];
                                            if(j >= (result_ndim - right_ndim))
                                            {
                                                int temp = loc;
                                                if(right_broadcast_dims[rdim] > 0)
                                                {
                                                    temp = 0;
                                                }
                                                ri += right_strides[rdim++] * temp;
                                            }
                                        }
                                        right_grad_ptr[ri] += result_grad_ptr[i] * alpha_;
                                    }
                                    Marshal.FreeHGlobal((IntPtr)left_strides);
                                    Marshal.FreeHGlobal((IntPtr)right_strides);
                                    Marshal.FreeHGlobal((IntPtr)result_strides);
                                    Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                                    Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                                };
                            }
                            else
                            {
                                Marshal.FreeHGlobal((IntPtr)left_strides);
                                Marshal.FreeHGlobal((IntPtr)right_strides);
                                Marshal.FreeHGlobal((IntPtr)result_strides);
                                Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                                Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                            }
                            return result;
                        }
                        case torchlite.int32:
                        {
                            if(!((alpha is byte) || (alpha is sbyte) ||
                                 (alpha is short) || (alpha is ushort) ||
                                 (alpha is int) || (alpha is uint) ||
                                 (alpha is long) || (alpha is ulong) ||
                                 (alpha is bool)))
                            {
                                throw new ArgumentException("For integer input tensors, argument alpha should be of integer or bool data type.");
                            }
                            int alpha_ = Convert.ToInt32(alpha);
                            var result = new Tensor(result_shape_, torchlite.int32);
                            var result_shape = result.shape.data_ptr;
                            var right_ptr = (int*)other.storage.data_ptr;
                            var result_ptr = (int*)result.storage.data_ptr;
                            for(int i = 0; i < result_numel; ++i)
                            {
                                var index = i;
                                int li = 0;
                                int ldim = 0;
                                int ri = 0;
                                int rdim = 0;
                                for(int j = 0; j < result_ndim; ++j)
                                {
                                    int loc = index / result_strides[j];
                                    index -= loc * result_strides[j];
                                    if(j >= (result_ndim - left_ndim))
                                    {
                                        int temp = loc;
                                        if(left_broadcast_dims[ldim] > 0)
                                        {
                                            temp = 0;
                                        }
                                        li += left_strides[ldim++] * temp;
                                    }
                                    if(j >= (result_ndim - right_ndim))
                                    {
                                        int temp = loc;
                                        if(right_broadcast_dims[rdim] > 0)
                                        {
                                            temp = 0;
                                        }
                                        ri += right_strides[rdim++] * temp;
                                    }
                                }
                                result_ptr[i] = left_ptr[li] + right_ptr[ri] * alpha_;
                            }
                            Marshal.FreeHGlobal((IntPtr)left_strides);
                            Marshal.FreeHGlobal((IntPtr)right_strides);
                            Marshal.FreeHGlobal((IntPtr)result_strides);
                            Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                            Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                            return result;
                        }
                        case torchlite.@bool:
                        {
                            if(!((alpha is byte) || (alpha is sbyte) ||
                                 (alpha is short) || (alpha is ushort) ||
                                 (alpha is int) || (alpha is uint) ||
                                 (alpha is long) || (alpha is ulong) ||
                                 (alpha is bool)))
                            {
                                throw new ArgumentException("For integer input tensors, argument alpha should be of integer or bool data type.");
                            }
                            int alpha_ = Convert.ToInt32(alpha);
                            var result = new Tensor(result_shape_, torchlite.int32);
                            var result_shape = result.shape.data_ptr;
                            var right_ptr = (byte*)other.storage.data_ptr;
                            var result_ptr = (int*)result.storage.data_ptr;
                            for(int i = 0; i < result_numel; ++i)
                            {
                                var index = i;
                                int li = 0;
                                int ldim = 0;
                                int ri = 0;
                                int rdim = 0;
                                for(int j = 0; j < result_ndim; ++j)
                                {
                                    int loc = index / result_strides[j];
                                    index -= loc * result_strides[j];
                                    if(j >= (result_ndim - left_ndim))
                                    {
                                        int temp = loc;
                                        if(left_broadcast_dims[ldim] > 0)
                                        {
                                            temp = 0;
                                        }
                                        li += left_strides[ldim++] * temp;
                                    }
                                    if(j >= (result_ndim - right_ndim))
                                    {
                                        int temp = loc;
                                        if(right_broadcast_dims[rdim] > 0)
                                        {
                                            temp = 0;
                                        }
                                        ri += right_strides[rdim++] * temp;
                                    }
                                }
                                result_ptr[i] = left_ptr[li] + right_ptr[ri] * alpha_;
                            }
                            Marshal.FreeHGlobal((IntPtr)left_strides);
                            Marshal.FreeHGlobal((IntPtr)right_strides);
                            Marshal.FreeHGlobal((IntPtr)result_strides);
                            Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                            Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                            return result;
                        }
                        default:
                        {
                            Marshal.FreeHGlobal((IntPtr)left_strides);
                            Marshal.FreeHGlobal((IntPtr)right_strides);
                            Marshal.FreeHGlobal((IntPtr)result_strides);
                            Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                            Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                            throw new NotImplementedException();
                        }
                    }
                }
                default:
                {
                    Marshal.FreeHGlobal((IntPtr)left_strides);
                    Marshal.FreeHGlobal((IntPtr)right_strides);
                    Marshal.FreeHGlobal((IntPtr)result_strides);
                    Marshal.FreeHGlobal((IntPtr)left_broadcast_dims);
                    Marshal.FreeHGlobal((IntPtr)right_broadcast_dims);
                    throw new NotImplementedException();
                }
            }
        }

    }

}