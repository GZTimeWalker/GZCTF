import { FC } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '@mantine/core'
import { mdiBackburger } from '@mdi/js'
import { Icon } from '@mdi/react'
import WithGameTab from '../../../../components/admin/WithGameTab'

const GameTeamReview: FC = () => {
  const navigate = useNavigate()
  return (
    <WithGameTab
      headProps={{ position: 'left' }}
      head={
        <Button
          leftIcon={<Icon path={mdiBackburger} size={1} />}
          onClick={() => navigate('/admin/games')}
        >
          返回上级
        </Button>
      }
    ></WithGameTab>
  )
}

export default GameTeamReview
