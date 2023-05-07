import { useSWRConfig } from 'swr'
import { useNavigate } from 'react-router'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import api from '@Api'

export const useUser = () => {
  const navigate = useNavigate()

  const {
    data: user,
    error,
    mutate,
  } = api.account.useAccountProfile({
    refreshInterval: 0,
    revalidateIfStale: false,
    onErrorRetry: (err, _key, _config, revalidate, { retryCount }) => {
      if (err?.status === 403) {
        api.account.accountLogOut().then(() => {
          navigate('/')
          showNotification({
            color: 'red',
            message: '账户已被禁用',
            icon: <Icon path={mdiClose} size={1} />,
          })
        })
        return
      }

      if (err?.status === 401) return

      if (retryCount >= 5) return

      setTimeout(() => revalidate({ retryCount: retryCount }), 10000)
    },
  })

  return { user, error, mutate }
}

export const useUserRole = () => {
  const { user, error } = useUser()
  return { role: user?.role, error }
}

export const useTeams = () => {
  const {
    data: teams,
    error,
    mutate,
  } = api.team.useTeamGetTeamsInfo({
    refreshInterval: 120000,
  })
  return { teams, error, mutate }
}

export const useLoginOut = () => {
  const navigate = useNavigate()
  const { mutate } = useSWRConfig()
  const { mutate: mutateProfile } = api.account.useAccountProfile()

  return () => {
    api.account.accountLogOut().then(() => {
      navigate('/')
      mutate((key) => typeof key === 'string' && key.includes('game/'), undefined, {
        revalidate: false,
      })
      mutateProfile(undefined, false)
      showNotification({
        color: 'teal',
        message: '登出成功',
        icon: <Icon path={mdiCheck} size={1} />,
      })
    })
  }
}
