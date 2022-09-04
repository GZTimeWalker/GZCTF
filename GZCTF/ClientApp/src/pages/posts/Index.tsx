import { FC } from 'react'
import { Stack } from '@mantine/core'
import StickyHeader from '@Components/StickyHeader'
import WithNavBar from '@Components/WithNavbar'
import { usePageTitle } from '@Utils/PageTitle'

const Posts: FC = () => {
  usePageTitle('文章')

  return (
    <WithNavBar>
      <StickyHeader />
      <Stack></Stack>
    </WithNavBar>
  )
}

export default Posts
