module Fabulous.StackAllocatedCollections

open System
open System.Collections.Generic
open System.Runtime.CompilerServices



//let a = Array.length [||]
//[<System.Runtime.CompilerServices.IsReadOnly>]


type Size =
    | Zero = 0uy
    | One = 1uy
    | Two = 2uy
    | Three = 3uy


[<Struct; NoComparison>]
type StackArray3<'v> =
    | Few of data: struct (Size * 'v * 'v * 'v)
    | Many of arr: 'v array

module StackArray3 =

    let inline empty () : StackArray3<'v> =
        Few(Size.Zero, Unchecked.defaultof<'v>, Unchecked.defaultof<'v>, Unchecked.defaultof<'v>)

    let inline one (v0: 'v) : StackArray3<'v> =
        Few(Size.One, v0, Unchecked.defaultof<'v>, Unchecked.defaultof<'v>)

    let inline two (v0: 'v, v1: 'v) : StackArray3<'v> =
        Few(Size.Two, v0, v1, Unchecked.defaultof<'v>)

    let inline three (v0: 'v, v1: 'v, v2: 'v) : StackArray3<'v> = Few(Size.Three, v0, v1, v2)

    let inline many (arr: 'v array) : StackArray3<'v> = Many arr

    let add (arr: StackArray3<'v> inref, v: 'v) : StackArray3<'v> =
        match arr with
        | Few (struct (size, v0, v1, v2)) ->
            match size with
            | Size.Zero -> one(v)
            | Size.One -> two(v0, v)
            | Size.Two -> three(v0, v1, v)
            | Size.Three -> many([| v0; v1; v2; v |])
            | _ -> empty() // should never happen but don't want to throw there
        | Many arr -> many(Array.appendOne v arr)


    let inline length (arr: StackArray3<'v> inref) : int =
        match arr with
        | Few (struct (size, _, _, _)) -> int size
        | Many arr -> arr.Length


    let get (arr: StackArray3<'v> inref) (index: int) : 'v =
        match arr with
        | Few (struct (size, v0, v1, v2)) ->
            if (index >= int size) then
                IndexOutOfRangeException() |> raise
            else
                match index with
                | 0 -> v0
                | 1 -> v1
                | _ -> v2

        | Many arr -> arr.[index]


    let find (test: 'v -> bool) (arr: StackArray3<'v> inref) : 'v =
        match arr with
        | Few (struct (size, v0, v1, v2)) ->
            match (size, test v0, test v1, test v2) with
            | Size.One, true, _, _
            | Size.Two, true, _, _
            | Size.Three, true, _, _ -> v0
            | Size.Two, false, true, _
            | Size.Three, false, true, _ -> v1
            | Size.Three, false, false, true -> v2
            | _ -> KeyNotFoundException() |> raise
        | Many arr -> Array.find test arr


    /// Note that you should always use the result,
    /// In Few mode it creates a new stack allocated array
    /// In Many case it sorts the Many variant inline for optimization reasons
    let rec inline sortInPlace<'T, 'V when 'V: comparison>
        ([<InlineIfLambda>] getKey: 'T -> 'V)
        (arr: StackArray3<'T> inref)
        : StackArray3<'T> =
        match arr with
        | Few (struct (size, v0, v1, v2)) ->
            match size with
            | Size.Zero
            | Size.One -> arr
            | Size.Two ->
                if (getKey v0 > getKey v1) then
                    two(v1, v0)
                else
                    arr
            | Size.Three ->
                match (getKey v0, getKey v1, getKey v1) with
                // abc acb bac bca cba cab

                //  a, c, b
                | a, b, c when a <= c && c <= b -> three(v0, v2, v1)

                //  b, a, c
                | a, b, c when b <= a && a <= c -> three(v1, v0, v2)

                //  b, c, a
                | a, b, c when b <= c && c <= a -> three(v1, v2, v0)

                //  c, b, a
                | a, b, c when c <= b && b <= a -> three(v2, v1, v0)

                //  c, a, b
                | a, b, c when c <= a && a <= b -> three(v2, v0, v1)

                // a, b, c left, thus already sorted
                | _ -> arr


            | _ -> empty() // should never happen but don't want to throw there
        | Many arr -> many(Array.sortInPlace getKey arr)


    let inline private arr0 () = [||]
    let inline private arr1 (v: 'v) = [| v |]
    let inline private arr2 (v0: 'v, v1: 'v) = [| v0; v1 |]
    let inline private arr3 (v0: 'v, v1: 'v, v2: 'v) = [| v0; v1; v2 |]

    let toArray (arr: StackArray3<'v> inref) : 'v array =
        match arr with
        | Few (struct (size, v0, v1, v2)) ->
            match size with
            | Size.Zero -> Array.empty
            | Size.One -> arr1 v0
            | Size.Two -> arr2(v0, v1)
            | _ -> arr3(v0, v1, v2)
        | Many arr -> arr


    let combine (a: StackArray3<'v>) (b: StackArray3<'v>) : StackArray3<'v> =
        match (a, b) with
        | (Few (struct (asize, a0, a1, a2)), Few (struct (bsize, b0, b1, b2))) ->
            match (asize, bsize) with
            | Size.Zero, _ -> b
            | _, Size.Zero -> a
            | Size.One, Size.One -> two(a0, b0)
            | Size.One, Size.Two -> three(a0, b0, b1)
            | Size.Two, Size.One -> three(a0, a1, b0)
            // now many cases
            | Size.One, Size.Three -> many([| a0; b0; b1; b2 |])
            | Size.Three, Size.One -> many([| a0; a1; a2; b0 |])
            | Size.Two, Size.Two -> many([| a0; a1; b0; b1 |])
            | Size.Three, Size.Two -> many([| a0; a1; a2; b0; b1 |])
            | Size.Two, Size.Three -> many([| a0; a1; b0; b1; b2 |])
            | Size.Three, Size.Three -> many([| a0; a1; a2; b0; b1; b2 |])
            | _ -> a // this should never happen because we exhausted all the other cases
        | Few _, Many arr2 -> many(Array.append(toArray &a) arr2) // TODO optimize
        | Many arr1, Few _ -> many(Array.append arr1 (toArray &b)) // TODO optimize
        | Many arr1, Many arr2 -> many(Array.append arr1 arr2)






module MutStackArray1 =

    [<IsByRefLike; Struct>]
    type T<'v> =
        | Empty
        | One of one: 'v
        | Many of struct (uint16 * 'v [])

    let addMut (arr: T<'v> inref, value: 'v) : T<'v> =
        match arr with
        | Empty -> One value
        | One v ->
            Many
                struct (2us,
                        [|
                            v
                            value
                            Unchecked.defaultof<'v>
                            Unchecked.defaultof<'v>
                        |])
        | Many struct (count, mutArr) ->
            if mutArr.Length > (int count) then
                // we can fit it in
                mutArr.[int count] <- value
                Many(count + 1us, mutArr)
            else
                // in this branch we reached the capacity of the array, thus needs to grow
                let countInt = int count

                let res =
                    // count is at least 2
                    // thus it is either going to grow at least by 1
                    // note that the growth rate is slower than ResizeArray
                    Array.zeroCreate(max((countInt * 2) / 3) countInt + 1)

                Array.blit mutArr 0 res 0 mutArr.Length
                res.[countInt] <- value
                Many(count + 1us, res)

    let toArray (arr: T<'v> inref) : 'v array =
        match arr with
        | Empty -> Array.empty
        | One v -> [| v |]
        | Many (struct (count, arr)) -> Array.take(int count) arr

    let toArraySlice (arr: T<'v> inref) : ArraySlice<'v> voption =
        match arr with
        | Empty -> ValueNone
        | One v -> ValueSome(1us, [| v |])
        | Many slice -> ValueSome slice

    let inline length (arr: T<'v> inref) : int =
        match arr with
        | Empty -> 0
        | One v -> 1
        | Many (struct (count, _)) -> int count

open FSharp.NativeInterop

let inline stackalloc<'a when 'a: unmanaged> (length: int) : Span<'a> =
    let p =
        NativePtr.stackalloc<'a> length
        |> NativePtr.toVoidPtr

    Span<'a>(p, length)

//let t = stackalloc<uint16>(3)



[<IsByRefLike>]
type DiffBuilder =
    struct
        val ops: Span<uint16>
        val mutable cursor: int
        val mutable rest: uint16 array

        new(span: Span<uint16>, cursor) =
            {
                ops = span // stackalloc<uint16>(capacity)
                cursor = cursor
                rest = null
            }
    end




module DiffBuilder =
    // 2 bytes
    type OpType =
        | Add // 1
        | Remove // 2
        | Change // 3

    // 00 is omitted on purpose for debuggability

    module OpCode =
        [<Literal>]
        let AddCode = 1us

        [<Literal>]
        let RemoveCode = 2us

        [<Literal>]
        let ChangeCode = 3us


    [<Struct>]
    type Op =
        | Added of added: uint16
        | Removed of removed: uint16
        | Changed of changed: uint16


    let inline create () = DiffBuilder(stackalloc<uint16>(8), 0)

    // reserve 2bits for op
    let valueMask = UInt16.MaxValue >>> 2

    // two left bits for op
    let opMask = UInt16.MaxValue ^^^ valueMask

    // TODO handle growth
    let addOpMut (builder: DiffBuilder byref) (op: OpType) (index: uint16) =
        let op =
            match op with
            | Add -> OpCode.AddCode
            | Remove -> OpCode.RemoveCode
            | Change -> OpCode.ChangeCode

        let encodedValue = ((uint16 op <<< 14) &&& opMask)
        let encodedValue = encodedValue ||| (index &&& valueMask)

        builder.ops.[builder.cursor] <- encodedValue
        builder.cursor <- builder.cursor + 1


    let inline lenght (builder: DiffBuilder byref) = builder.cursor

    let inline private decode (encodedValue: uint16) : Op =
        let op = encodedValue >>> 14

        let value = encodedValue &&& valueMask

        match op with
        | OpCode.AddCode -> Added(value)
        | OpCode.RemoveCode -> Removed(value)
        | OpCode.ChangeCode -> Changed(value)
        | _ -> IndexOutOfRangeException() |> raise

    let inline toArray (builder: DiffBuilder byref) (map: Op -> 't) : 't array =
        let len = lenght &builder
        let res = Array.zeroCreate<'t> len

        for i = 0 to len - 1 do
            res.[i] <- map(decode builder.ops.[i])

        res
