import { useState } from 'react'

export interface ArrayResponse<T> {
  /** 数据 */
  data: T[]
  /**
   * 数据长度
   * @format int32
   */
  length: number
  /**
   * 总长度
   * @format int32
   */
  total?: number
}

export function useArrayResponse<T>() {
  const [data, setInnerData] = useState<T[]>()
  const [total, setTotal] = useState(0)
  const [length, setLength] = useState(0)

  const setData = (res: ArrayResponse<T>) => {
    setInnerData(res.data)
    setTotal(res.total ?? 0)
    setLength(res.length)
  }

  const updateData = (newData: T[]) => {
    setInnerData(newData)
    setLength(newData.length)
  }

  return { data, total, length, setData, updateData }
}
