import React, { FC, useEffect } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { Center, Loader } from '@mantine/core'
import { useUserRole } from '@Utils/useUser'
import { Role } from '@Api'

interface WithRoleProps {
  requiredRole: Role
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

const WithRole: FC<WithRoleProps> = ({ requiredRole, children }) => {
  const { role, error } = useUserRole()
  const navigate = useNavigate()
  const location = useLocation()

  const required = RoleMap.get(requiredRole)!

  useEffect(() => {
    if (error && error.status === 401)
      navigate(`/account/login?from=${location.pathname}`, { replace: true })

    if (!role)
      return

    const current = RoleMap.get(role)!

    if (current < required) navigate('/404')
  }, [role, error, required, navigate])

  if (role && RoleMap.get(role)! < required /* show loader before redirect */) {
    return (
      <Center style={{ height: 'calc(100vh - 32px)' }}>
        <Loader />
      </Center>
    )
  }

  return <>{children}</>
}

export default WithRole
