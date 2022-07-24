import { FC } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button } from '@mantine/core'
import { mdiBackburger } from '@mdi/js'
import { Icon } from '@mdi/react'
import WithGameTab from '../../../../../components/admin/WithGameTab'

const GameChallengeEdit: FC = () => {
  const navigate = useNavigate()
  const { id, chalId } = useParams()
  return (
    <WithGameTab
      headProps={{ position: 'left' }}
      head={
        <Button
          leftIcon={<Icon path={mdiBackburger} size={1} />}
          onClick={() => navigate(`/admin/games/${id}/info`)}
        >
          返回上级
        </Button>
      }
    ></WithGameTab>
  )
}

export default GameChallengeEdit
