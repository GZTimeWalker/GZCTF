import React, { FC } from 'react'
import { Role } from '../../Api'
import WithNavBar from '../../components/WithNavbar'
import WithRole from '../../components/WithRole'
import WithAdminTab from './WithAdminTab'
import { AdminTabProps } from './WithAdminTab'

const AdminPage: FC<AdminTabProps> = (props) => {
  return (
    <WithNavBar width="90%">
      <WithRole requiredRole={Role.Admin}>
        <WithAdminTab {...props} />
      </WithRole>
    </WithNavBar>
  )
}

export default AdminPage
