import { Center, Loader } from '@mantine/core'
import React, { FC, useEffect } from 'react'
import { useLocation, useNavigate } from 'react-router'
import { useUser } from '@Hooks/useUser'
import { Role } from '@Api'

interface WithRoleProps {
  requiredRole: Role
  allowEventAdmin?: boolean
  children?: React.ReactNode
}

export const RoleMap = new Map<Role, number>([
  [Role.Admin, 3],
  [Role.Monitor, 1],
  [Role.User, 0],
  [Role.Banned, -1],
])

export const RequireRole = (requiredRole: Role, role?: Role | null) =>
  RoleMap.get(role ?? Role.User)! >= RoleMap.get(requiredRole)!

export const WithRole: FC<WithRoleProps> = ({ requiredRole, allowEventAdmin, children }) => {
  const { user, error } = useUser()
  const navigate = useNavigate()
  const location = useLocation()

  const required = RoleMap.get(requiredRole)!
  const role = user?.role

  useEffect(() => {
    if (error && error.status === 401) {
      navigate(`/account/login?from=${location.pathname}`, { replace: true })
    }

    if (!role) return

    const current = RoleMap.get(role)!

    if (current < required) {
      if (allowEventAdmin && user?.hasManagedGames) {
        return
      }
      navigate('/404')
    }
  }, [role, error, required, navigate, allowEventAdmin, user?.hasManagedGames])

  const current = role ? RoleMap.get(role)! : -1
  if (role && current < required && !(allowEventAdmin && user?.hasManagedGames)) {
    return (
      <Center h="calc(100vh - 32px)">
        <Loader />
      </Center>
    )
  }

  return <>{children}</>
}
