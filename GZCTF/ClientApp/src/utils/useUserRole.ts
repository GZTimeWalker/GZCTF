import api, { Role } from '@Api'

export const useUserRole = () => {
  const { data, error } = api.account.useAccountProfile({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  return { role: data?.role ?? Role.User, error }
}
