import { showNotification, updateNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import { AxiosError, AxiosResponse } from 'axios'
import { ContentType } from '@Api'

const openAxiosBlobResponse = (res: AxiosResponse, downloadFilename?: string) => {
  if (res.data instanceof Blob) {
    const blobURL = window.URL.createObjectURL(res.data)
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

const handleAxiosBlobError = async (err: AxiosError) => {
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
}

export const downloadBlob = (
  promise: Promise<AxiosResponse>,
  filename: string | undefined,
  setDisabled: (value: React.SetStateAction<boolean>) => void,
  t: (key: string) => string
) => {
  setDisabled(true)

  showNotification({
    color: 'orange',
    id: 'blob-download',
    message: t('common.download.started'),
    loading: true,
    autoClose: false,
  })

  promise
    .then((res) => {
      updateNotification({
        id: 'blob-download',
        color: 'teal',
        message: t('common.download.success'),
        icon: <Icon path={mdiCheck} size={1} />,
        loading: false,
        autoClose: true,
      })
      openAxiosBlobResponse(res, filename)
    })
    .catch(async (err: AxiosError) => {
      updateNotification({
        id: 'blob-download',
        color: 'red',
        title: t('common.download.failed'),
        message: await handleAxiosBlobError(err),
        icon: <Icon path={mdiClose} size={1} />,
        autoClose: false,
        withCloseButton: true,
      })
    })
    .finally(() => {
      setDisabled(false)
    })
}

export const tryGetErrorMsg = (err: any, t: (key: string) => string) => {
  return err?.response?.data?.title ?? err?.title ?? err ?? t('common.error.unknown')
}

export const showErrorNotification = (err: any, t: (key: string) => string) => {
  if (err?.response?.status === 429) {
    showNotification({
      color: 'red',
      title: t('common.error.try_later'),
      message: tryGetErrorMsg(err, t),
      icon: <Icon path={mdiClose} size={1} />,
    })
    return
  }

  console.warn(err)
  showNotification({
    color: 'red',
    title: t('common.error.encountered'),
    message: tryGetErrorMsg(err, t),
    icon: <Icon path={mdiClose} size={1} />,
  })
}
