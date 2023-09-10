import React, { FC } from 'react'
import WithNavBar from '@Components/WithNavbar'
import WithRole from '@Components/WithRole'
import WithAdminTab, { AdminTabProps } from '@Components/admin/WithAdminTab'
import { Role } from '@Api'

const AdminPage: FC<AdminTabProps> = (props) => {
  return (
    <WithNavBar width="90%" minWidth={1080}>
      <WithRole requiredRole={Role.Admin}>
        <WithAdminTab {...props} />
      </WithRole>
    </WithNavBar>
  )
}

export default AdminPage
