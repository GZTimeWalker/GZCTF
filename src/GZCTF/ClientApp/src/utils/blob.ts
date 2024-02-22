import { AxiosError, AxiosResponse } from 'axios'
import { ContentType } from '@Api'

type WindowOpenParameters =
  Parameters<typeof window.open> extends [any?, ...infer Rest] ? Rest : never

export const openBlob = (blob: Blob, ...rest: WindowOpenParameters) => {
  let blobURL = window.URL.createObjectURL(blob)
  window.open(blobURL, ...rest)
  window.URL.revokeObjectURL(blobURL)
}

export const openAxiosBlobResponse = (res: AxiosResponse) => {
  if (res.data instanceof Blob) {
    openBlob(res.data, '_blank')
  } else {
    throw new Error('Response data is not a Blob')
  }
}

export const handleAxiosBlobError = async (err: AxiosError) => {
  if (err.response?.data instanceof Blob) {
    const data = err.response.data
    switch (data.type) {
      case ContentType.Text:
        return await data.text()
      case ContentType.Json:
        const res = JSON.parse(await data.text())
        if (res.title) {
          return res.title
        }
    }
  }
  return err.message
}
