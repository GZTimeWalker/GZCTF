import { showNotification, updateNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { AxiosError, AxiosResponse } from 'axios'
import { ContentType } from '@Api'

export const handleAxiosError = async (err: unknown) => {
  if (err instanceof AxiosError) {
    if (err.response?.data instanceof Blob) {
      const data = err.response.data

      if (data.type === ContentType.Text) {
        return await data.text()
      }

      if (data.type === ContentType.Json) {
        const res = window.JSON.parse(await data.text())
        if (res.title) {
          return res.title
        }
      }
    }
    return err.message
  } else {
    return String(err)
  }
}

const openAxiosBlobResponse = (res: AxiosResponse, downloadFilename?: string) => {
  if (res.data instanceof Blob) {
    const blobURL = window.URL.createObjectURL(res.data)
    const contentDisposition = res.headers['content-disposition']

    const extractFilename = (header: string): string | undefined => {
      const filenameStarMatch = header.match(/filename\*=UTF-8''([^;]+)/i)
      if (filenameStarMatch) {
        return decodeURIComponent(filenameStarMatch[1])
      }
      const filenameMatch = header.match(/filename="?([^";]+)"?/)
      if (filenameMatch) {
        return filenameMatch[1]
      }
      return undefined
    }

    if (!downloadFilename && contentDisposition) {
      downloadFilename = extractFilename(contentDisposition)
    }

    const anchor = document.createElement('a')
    anchor.style.display = 'none'
    anchor.href = blobURL

    if (downloadFilename) {
      anchor.download = downloadFilename
    }

    document.body.appendChild(anchor)
    anchor.click()
    window.setTimeout(() => {
      anchor.remove()
      window.URL.revokeObjectURL(blobURL)
    })
  } else {
    throw new Error('Response data is not a Blob')
  }
}

export const downloadBlob = async (
  promise: Promise<AxiosResponse>,
  setDisabled: (value: React.SetStateAction<boolean>) => void,
  t: (key: string) => string,
  filename?: string
) => {
  setDisabled(true)

  showNotification({
    color: 'orange',
    id: 'blob-download',
    message: t('common.download.started'),
    loading: true,
    autoClose: false,
  })

  try {
    const res = await promise
    updateNotification({
      id: 'blob-download',
      color: 'teal',
      message: t('common.download.success'),
      icon: <Icon path={mdiCheck} size={1} />,
      loading: false,
      autoClose: true,
    })
    openAxiosBlobResponse(res, filename)
  } catch (err) {
    updateNotification({
      id: 'blob-download',
      color: 'red',
      title: t('common.download.failed'),
      message: await handleAxiosError(err),
      icon: <Icon path={mdiClose} size={1} />,
      autoClose: false,
      withCloseButton: true,
    })
  } finally {
    setDisabled(false)
  }
}
